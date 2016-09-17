using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal class AddAreaToJoinConditionVisitor : SingleQuerySqlVisitorBase
    {
        private TableDeclarationClause mainTable;

        public override JoinClause VisitJoin(JoinClause clause)
        {
            var joinTable = (TableDeclarationClause) clause.Source;
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
                        Table = joinTable
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
    }
}