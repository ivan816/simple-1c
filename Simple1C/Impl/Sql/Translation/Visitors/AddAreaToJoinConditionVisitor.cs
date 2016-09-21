using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Impl.Sql.Translation.QueryEntities;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal class AddAreaToJoinConditionVisitor : SqlVisitor
    {
        private TableDeclarationClause mainTable;

        public override JoinClause VisitJoin(JoinClause clause)
        {
            if (mainTable == null || !(clause.Source is TableDeclarationClause))
                return clause;

            clause.Condition = new AndExpression
            {
                Left = new EqualityExpression
                {
                    Left = new ColumnReferenceExpression
                    {
                        Name = PropertyNames.Area,
                        Table = mainTable
                    },
                    Right = new ColumnReferenceExpression
                    {
                        Name = PropertyNames.Area,
                        Table = clause.Source
                    }
                },
                Right = clause.Condition
            };
            return clause;
        }

        public override ISqlElement VisitTableDeclaration(TableDeclarationClause clause)
        {
            mainTable = clause;
            return clause;
        }

        public override SubqueryTable VisitSubqueryTable(SubqueryTable subqueryTable)
        {
            mainTable = null;
            return subqueryTable;
        }
    }
}