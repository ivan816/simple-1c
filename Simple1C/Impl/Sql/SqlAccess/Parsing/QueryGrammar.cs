using Irony.Ast;
using Irony.Parsing;
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

            var selectStmt = new NonTerminal("selectStmt", Use<SelectClause>());
            var columnItemList = new NonTerminal("columnItemList", Use<ElementsHolder>());
            var columnItem = new NonTerminal("columnItem", Use<SelectColumn>());
            var fromClauseOpt = new NonTerminal("fromClauseOpt", Use<DeclarationClause>());
            var idlist = new NonTerminal("idlist", Use<ElementsHolder>());
            var id = new NonTerminal("id", Use<Identifier>());
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

        private static AstNodeCreator Use<T>()
            where T : new()
        {
            return delegate(AstContext context, ParseTreeNode node)
            {
                var astNode = new T();
                var init = astNode as IAstNodeInit;
                if (init != null)
                    init.Init(context, node);
                node.AstNode = astNode;
            };
        }
    }
}