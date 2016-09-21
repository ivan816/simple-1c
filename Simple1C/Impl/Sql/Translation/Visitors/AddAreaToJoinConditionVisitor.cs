using Simple1C.Impl.Sql.SqlAccess.Syntax;

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
                        Name = "ОбластьДанныхОсновныеДанные",
                        Table = mainTable
                    },
                    Right = new ColumnReferenceExpression
                    {
                        Name = "ОбластьДанныхОсновныеДанные",
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