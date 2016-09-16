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
                throw new InvalidOperationException(FormatErrors(parseTree, parser.Context.TabWidth));
            var result = (RootClause) parseTree.Root.AstNode;
            new ColumnReferenceTableNameRewriter().Visit(result);
            return result;
        }

        private static string FormatErrors(ParseTree parseTree, int tabWidth)
        {
            var b = new StringBuilder();
            foreach (var message in parseTree.ParserMessages)
            {
                b.AppendLine(string.Format("{0}: {1} at {2} in state {3}", message.Level, message.Message,
                    message.Location, message.ParserState));

                var theMessage = message;
                var lines = parseTree.SourceText.Replace("\t", new string(' ', tabWidth))
                    .Split(new[] {"\r\n"}, StringSplitOptions.None)
                    .Select((sourceLine, index) =>
                        index == theMessage.Location.Line
                            ? string.Format("{0}\r\n{1}|<-Here", sourceLine,
                                new string('_', theMessage.Location.Column))
                            : sourceLine);
                foreach (var line in lines)
                    b.AppendLine(line);
            }
            throw new InvalidOperationException(string.Format("parse errors\r\n:{0}", b));
        }
    }
}