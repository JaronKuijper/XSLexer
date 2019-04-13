﻿using System.Collections.Generic;
using XSLexer.Data;
using System.Text;
using System;
using System.Text.RegularExpressions;

namespace XSLexer.Lexer
{
    // This should probably be state machined somehow if we want more
    class Tokenizer
    {
        private DataContainer[] m_Previous = new DataContainer[0];
        private DataContainer[] m_Current = new DataContainer[0];

        private List<Token> m_Tokens = new List<Token>();
        private StringBuilder m_Buffer = new StringBuilder(32);

        private string m_Code = "";
        private int m_Index = 0;

        private string m_SearchFlag = "";
        private bool m_SearchInclusive = false;

        private LexConfig m_Config;

        public Tokenizer(LexConfig data)
        {
            m_Config = data;
        }

        public void Reset()
        {
            ClearBuffers();
            m_Tokens.Clear();
            m_Code = "";
            m_Index = 0;
        }

        public Token[] Tokenize(string code)
        {
            m_Code = code + '\n';

            while (CreateNextToken(out Token token))
            {
                m_Tokens.Add(token);

                if (!string.IsNullOrEmpty(m_SearchFlag))
                    m_Tokens.Add(ParseUntil(m_SearchFlag, token));

            }

            Token[] TokenArray = m_Tokens.ToArray();

            Reset();
            return TokenArray;
        }

        public bool CreateNextToken(out Token token)
        {
            token = null;
            for (; m_Index < m_Code.Length; m_Index++)
            {
                m_Buffer.Append(m_Code[m_Index]);

                m_Current = GetAvailableTokens(m_Buffer.ToString(), false);
                if (m_Current.Length == 0 && m_Previous.Length == 0)
                {
                    SkipUnknownChar();
                }
                else if (m_Current.Length == 0 && m_Previous.Length > 0)
                {
                    token = CreateToken();
                    return true;
                }
                else if (m_Current.Length > 0 && m_Previous.Length > 0)
                {
                    if (!Overlaps())
                    {
                        token = CreateToken();
                        return true;
                    }
                }

                m_Previous = m_Current;
            }
            return false;
        }

        private void SkipUnknownChar()
        {
            m_Buffer.Remove(m_Buffer.Length - 1, 1);
        }

        private Token ParseUntil(string searchType, Token lastToken)
        {
            string lastType = lastToken.Type;
            string lastValue = lastToken.Value;
            m_Tokens.RemoveAt(m_Tokens.Count - 1);

            int startIndex = m_Index - lastValue.Length;

            while (CreateNextToken(out Token t))
            {
                if (t.Type == searchType)
                {
                    if (!m_SearchInclusive)
                        m_Index -= t.Value.Length;

                    int length = m_Index - startIndex;
                    m_SearchFlag = "";
                    return new Token(lastType, m_Code.Substring(startIndex, length));
                }
            }
            throw new Exception("Tried parsing until I couldn't");
        }

        private bool Overlaps()
        {
            DataSet current = new DataSet("CurrentSet", m_Current);
            DataSet previous = new DataSet("PreviousSet", m_Previous);

            for (int j = 0; j < current.Length; j++)
            {
                if (!previous.GetSet(current.GetSet(j).Name).IsEmpty)
                    return true;
            }

            return false;
        }

        private void ClearBuffers()
        {
            m_Previous = new DataContainer[0];
            m_Current = new DataContainer[0];
            m_Buffer.Clear();
        }


        private Token CreateToken()
        {
            m_Buffer.Remove(m_Buffer.Length - 1, 1);
            DataContainer[] Final = GetAvailableTokens(m_Buffer.ToString(), true);

            if (Final.Length > 1)
                throw new Exception("Found ambigious first phase tokens! ");

            if (Final.Length == 0)
                throw new Exception("Tried saving token with no previous containers!");

            Token token = new Token(Final[0].Name, m_Buffer.ToString());

            if (string.IsNullOrEmpty(m_SearchFlag))
            {
                if (Final[0].HasKey(TokenConsts.KEYWORD_UNTIL))
                {
                    m_SearchFlag = Final[0].GetValue(TokenConsts.KEYWORD_UNTIL).value;
                    m_SearchInclusive = false;
                }
            }

            if (string.IsNullOrEmpty(m_SearchFlag))
            {
                if (Final[0].HasKey(TokenConsts.KEYWORD_UNTILWITH))
                {
                    m_SearchFlag = Final[0].GetValue(TokenConsts.KEYWORD_UNTILWITH).value;
                    m_SearchInclusive = true;
                }
            }

            // Clear buffering
            ClearBuffers();

            return token;
        }

        private DataContainer[] GetAvailableTokens(string checkString, bool final)
        {
            if (string.IsNullOrEmpty(checkString))
                return new DataContainer[0];

            Func<DataContainer, bool> TokensPotential = (x) => TokenValidationPredicates.Potential(x, checkString);
            Func<DataContainer, bool> TokensFinal = (x) => TokenValidationPredicates.Final(x, checkString);

            return m_Config.Tokens.Filter(final ? TokensFinal : TokensPotential);
        }
    }
}