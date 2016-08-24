using Irony.Parsing;
using Simple1C.Impl.Sql;

namespace Simple1C.Tests.Sql
{
    public class NestedSyntaxToJoinRewriter : ParseTreeVisitor
    {
        private readonly MappingSchema schema;
        private readonly QueryGrammar grammar;

        public NestedSyntaxToJoinRewriter(MappingSchema schema, QueryGrammar grammar)
        {
            this.schema = schema;
            this.grammar = grammar;
        }

        public override void Visit(ParseTreeNode n)
        {
            if (n.Term == grammar.SelectClause)
            {
                var nSelectList = GetChild(n, grammar.SelectList);
                var nColumnItemList = GetChild(nSelectList, grammar.ColumnItemList);
                foreach (var nColumn in n.ChildNodes)
                {
                    var nColumnSource = GetChild(nColumn,grammar.ColumnSource);
                    var nId = GetChild(nColumnSource, grammar.Id);
                }
            }
            else
                base.Visit(n);
        }
    }
}