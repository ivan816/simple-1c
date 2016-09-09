using System;
using System.Text;
using Irony.Parsing;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    internal class QueryParser
    {
        public SelectClause Parse(string source)
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
            var parser = new Parser(languageData);
            var parseTree = parser.Parse(source);
            if (parseTree.Status != ParseTreeStatus.Parsed)
            {
                var b = new StringBuilder();
                foreach (var message in parseTree.ParserMessages)
                    b.AppendFormat("{0}\t{1} {2} {3}", message.Message, message.Location,
                        message.Level, message.ParserState);
                throw new InvalidOperationException(string.Format("parse error\r\n{0}", b));
            }
            return (SelectClause) parseTree.Root.AstNode;
        }
    }
}