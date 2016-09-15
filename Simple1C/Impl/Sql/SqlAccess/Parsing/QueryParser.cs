using System;
using System.Linq;
using System.Text;
using Irony.Parsing;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    internal class QueryParser
    {
        private readonly Parser parser;

        public QueryParser()
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
            parser = new Parser(languageData);
        }

        public RootClause Parse(string source)
        {
            var parseTree = parser.Parse(source);
            if (parseTree.Status != ParseTreeStatus.Parsed)
            {
                var b = new StringBuilder();
                foreach (var message in parseTree.ParserMessages)
                {
                    b.AppendLine(string.Format("{0}: {1} at {2} in state {3}", message.Level, message.Message,
                        message.Location, message.ParserState));

                    var lines = source.Split(new[] {"\r\n"}, StringSplitOptions.None)
                        .Select((sourceLine, index) =>
                            index == message.Location.Line
                                ? string.Format("{0}\r\n{1}|<-Here", sourceLine,
                                    new string('_', message.Location.Column))
                                : sourceLine);
                    foreach (var line in lines)
                        b.AppendLine(line);
                }
                throw new InvalidOperationException(string.Format("parse error\r\n{0}", b));
            }
            var result = (RootClause) parseTree.Root.AstNode;
            new ColumnReferenceTableNameRewriter().Visit(result);
            return result;
        }
    }
}