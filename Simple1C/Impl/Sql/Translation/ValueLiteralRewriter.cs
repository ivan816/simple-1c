using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation
{
    internal class ValueLiteralRewriter : SingleSelectSqlVisitorBase
    {
        private readonly QueryEntityAccessor queryEntityAccessor;
        private readonly QueryEntityRegistry queryEntityRegistry;

        public ValueLiteralRewriter(QueryEntityAccessor queryEntityAccessor,
            QueryEntityRegistry queryEntityRegistry)
        {
            this.queryEntityAccessor = queryEntityAccessor;
            this.queryEntityRegistry = queryEntityRegistry;
        }

        public override ISqlElement VisitValueLiteral(ValueLiteralExpression expression)
        {
            var enumValueItems = expression.ObjectName.Split('.');
            var table = queryEntityRegistry.CreateQueryEntity(null, enumValueItems[0] + "." + enumValueItems[1]);
            var selectClause = new SelectClause {Source = queryEntityAccessor.GetTableDeclaration(table)};
            selectClause.Fields.Add(new SelectFieldElement
            {
                Expression = new ColumnReferenceExpression
                {
                    Name = table.GetSingleColumnName("Ссылка"),
                    Declaration = (TableDeclarationClause) selectClause.Source
                }
            });
            var enumMappingsJoinClause = queryEntityAccessor.CreateEnumMappingsJoinClause(table);
            selectClause.JoinClauses.Add(enumMappingsJoinClause);
            selectClause.WhereExpression = new EqualityExpression
            {
                Left = new ColumnReferenceExpression
                {
                    Name = "enumValueName",
                    Declaration = (TableDeclarationClause) enumMappingsJoinClause.Source
                },
                Right = new LiteralExpression
                {
                    Value = enumValueItems[2]
                }
            };
            return new SubqueryClause {SelectClause = selectClause};
        }
    }
}