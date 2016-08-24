using System.Text;
using Irony.Parsing;

namespace Simple1C.Tests.Sql
{
    public class SqlFormatter : ParseTreeVisitor
    {
        private readonly StringBuilder builder = new StringBuilder();
        private readonly QueryGrammar grammar;
        private int indent;

        public SqlFormatter()
        {
            grammar = SqlParseHelpers.GetGrammar();
        }

        public static string Format(ParseTreeNode node)
        {
            var formatter = new SqlFormatter();
            formatter.Visit(node);
            return formatter.builder.ToString();
        }

        public override void Visit(ParseTreeNode n)
        {
            if (n.Term == grammar.SelectClause)
            {
                builder.AppendLine("select");
                WriteColumns(n);
                builder.AppendLine();
                builder.AppendLine("from");
                var fromClause = GetChild(n, grammar.FromClauseOpt);
                var tableIds = GetChild(fromClause, grammar.IdList);
                foreach (var id in tableIds.ChildNodes)
                {
                    WriteIndent();
                    WriteId(id);
                }
               }
            else
                base.Visit(n);
        }

        private void WriteColumns(ParseTreeNode n)
        {
            indent++;
            var nSelectList = GetChild(n, grammar.SelectList);
            var nColumnItemList = GetChild(nSelectList, grammar.ColumnItemList);
            var isFirst = true;
            foreach (var nColumn in nColumnItemList.ChildNodes)
            {
                if (isFirst)
                    isFirst = false;
                else
                {
                    builder.Append(',');
                    builder.AppendLine();
                }
                var nColumnSource = GetChild(nColumn, grammar.ColumnSource);
                var nId = GetChild(nColumnSource, grammar.Id);
                WriteIndent();
                WriteId(nId);
                var nAlias = GetChild(nColumn, grammar.AliasOpt);
                var nAliasId = GetChild(nAlias, grammar.Id);
                builder.Append(" as ");
                WriteId(nAliasId);
            }
        }

        private void WriteIndent()
        {
            if (indent > 0)
                builder.Append(new string('\t', indent));
        }

        private void WriteId(ParseTreeNode nId)
        {
            var isFirst = true;
            foreach (var idItem in nId.ChildNodes)
            {
                if (isFirst)
                    isFirst = false;
                else
                    builder.Append('.');
                builder.Append(idItem.Token.Text);
            }
        }
    }
}