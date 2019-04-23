﻿using System.Collections.Generic;

namespace XSLexer.Grammar
{
    /// <summary>
    /// Parses StructureRule tree
    /// </summary>
    static class GrammarRulesetParser
    {
        private static int m_Line = 0;
        private static char[] TRIM = { ' ', '\r', '\n', '\t' };

        public static GrammarRuleset Parse(string code)
        {
            m_Line = 0;
            string[] lines = code.Split('\n');
            PartialGrammarRuleset partialGrammarConfiguration = new PartialGrammarRuleset();
            for (int i = 0; i < lines.Length; i++)
            {
                m_Line++;
                if (string.IsNullOrWhiteSpace(lines[i]) || lines[i][0] == '#')
                    continue;
                partialGrammarConfiguration.Add(ParseRule(lines[i]));
            }
            return new GrammarRuleSetFinalizer().Finalize(partialGrammarConfiguration);
        }

        // *************************** RULE PARSING *************************** 
        private static PartialBaseGrammarRule ParseRule(string line)
        {
            string[] Struct = line.Split('=');
            PartialBaseGrammarRule baseRule = new PartialBaseGrammarRule();

            if (Struct[0][0] == '>')
            {
                baseRule.IsRoot = true;
                baseRule.Name = Struct[0].Remove(0, 1).Trim(TRIM);
            }
            else
            {
                baseRule.Name = Struct[0].Trim(TRIM);
            }

            baseRule.GrammarRuleValue = ParseValues(Struct[1]);
            baseRule.Line = m_Line;
            return baseRule;
        }



        // *************************** VALUE PARSING *************************** 
        private static PartialGrammarRuleValue ParseValues(string line)
        {
            return ParseValues(line.Split(':'));
        }

        private static PartialGrammarRuleValue ParseValues(string[] values)
        {
            PartialGrammarRuleValue root;
            PartialGrammarRuleValue newest;

            newest = ParseValue(values[0].Trim(TRIM));
            root = newest;

            for (int i = 1; i < values.Length; i++)
            {
                newest.Next = ParseValue(values[i].Trim(TRIM));
                newest = newest.Next;
            }
            return root;
        }

        private static PartialGrammarRuleValue ParseValue(string value)
        {
            return ParseValue(new PartialGrammarRuleValue(), value);
        }

        private static PartialGrammarRuleValue ParseValue(PartialGrammarRuleValue rule, string value)
        {
            if (value[0] == '*')
            {
                rule.IsMultiple = true;
                return ParseValue(rule, value.Remove(0, 1));
            }

            if (value[0] == '!')
            {
                rule.IsReferenceType = true;
                return ParseValue(rule, value.Remove(0, 1));
            }

            string[] split = value.Split('&');
            if (split.Length == 2)
            {
                rule.Type = split[0];
                rule.Value = split[1];
                rule.HasValue = true;
                return rule;
            }

            rule.Type = value;
            return rule;
        }
    }
}
