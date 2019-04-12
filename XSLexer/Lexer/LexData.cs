﻿using XSLexer.Data;

namespace XSLexer.Lexer
{
    class LexData
    {
        public TokenDataSet Tokens { get; }
        public WordListDataSet WordDefinitions { get; }

        public LexData(DataSet[] tokenDataSets, DataSet[] WordDefinitions)
        {
            Tokens = new TokenDataSet(tokenDataSets);
            this.WordDefinitions = new WordListDataSet(WordDefinitions);
        }
    }
}
