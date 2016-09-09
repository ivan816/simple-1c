using System;
using System.Linq;
using Irony.Ast;
using Irony.Parsing;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    public class QueryGrammar : Grammar
    {
        public QueryGrammar()
            : base(false)
        {
            LanguageFlags = LanguageFlags.CreateAst;

            var termComma = ToTerm(",");
            var dot = ToTerm(".");
            var termSelect = ToTerm("SELECT");
            var termFrom = ToTerm("FROM");

            var selectStmt = NonTerminal("selectStmt", delegate(ParseTreeNode n)
            {
                var elements = n.Elements();
                var result = new SelectClause {Table = elements.OfType<DeclarationClause>().Single()};
                foreach (var c in elements.OfType<SelectColumn>())
                {
                    ((ColumnReferenceExpression) c.Expression).TableName = result.Table.Name;
                    result.Columns.Add(c);
                }
                return result;
            });
            var columnItemList = NonTerminal("columnItemList");
            var columnItem = NonTerminal("columnItem", n => new SelectColumn
            {
                Expression = new ColumnReferenceExpression {Name = n.Elements().OfType<Identifier>().Single().Value}
            });
            var fromClauseOpt = NonTerminal("fromClauseOpt", n => new DeclarationClause
            {
                Name = n.Elements().OfType<Identifier>().Single().Value
            });
            var idlist = NonTerminal("idlist");
            var id = NonTerminal("id", n => new Identifier
            {
                Value = n.ChildNodes.Select(x => x.Token.ValueString).JoinStrings(".")
            });

            var idSimple = new IdentifierTerminal("Identifier");
            idSimple.SetFlag(TermFlags.NoAstNode);

            selectStmt.Rule = termSelect + columnItemList + fromClauseOpt;
            columnItemList.Rule = MakePlusRule(columnItemList, termComma, columnItem);
            columnItem.Rule = id;
            fromClauseOpt.Rule = termFrom + idlist;
            idlist.Rule = MakePlusRule(idlist, termComma, id);
            id.Rule = MakePlusRule(id, dot, idSimple);
            Root = selectStmt;
        }

        private static NonTerminal NonTerminal(string name)
        {
            return NonTerminal(name, n => new ElementsHolder
            {
                Elements = n.Elements()
            });
        }

        private static NonTerminal NonTerminal(string name, Func<ParseTreeNode, object> creator)
        {
            return new NonTerminal(name, delegate(AstContext context, ParseTreeNode n) { n.AstNode = creator(n); });
        }
    }
}