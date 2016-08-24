using System;
using System.Text;
using Irony.Parsing;

namespace Simple1C.Tests.Sql
{
    public static class SqlParseHelpers
    {
        private static LanguageData language;
        private static readonly object lockObject = new object();

        public static QueryGrammar GetGrammar()
        {
            return (QueryGrammar) GetLanguage().Grammar;
        }

        public static LanguageData GetLanguage()
        {
            if (language == null)
                lock (lockObject)
                    if (language == null)
                    {
                        var queryGrammar = new QueryGrammar();
                        var languageData = new LanguageData(queryGrammar);
                        if (languageData.Errors.Count > 0)
                        {
                            var b = new StringBuilder();
                            foreach (var error in languageData.Errors)
                                b.Append(error);
                            throw new InvalidOperationException(string.Format("invalid grammar\r\n{0}", b));
                        }
                        language = languageData;
                    }
            return language;
        }

        public static ParseTreeNode Parse(string text)
        {
            var parser = new Parser(GetLanguage());
            var parseTree = parser.Parse(text);
            if (parseTree.Status != ParseTreeStatus.Parsed)
            {
                var b = new StringBuilder();
                foreach (var message in parseTree.ParserMessages)
                    b.AppendFormat("{0}\t{1} {2} {3}", message.Message, message.Location,
                        message.Level, message.ParserState);
                throw new InvalidOperationException(string.Format("parse error\r\n{0}", b));
            }
            return parseTree.Root;
        }
    }
}