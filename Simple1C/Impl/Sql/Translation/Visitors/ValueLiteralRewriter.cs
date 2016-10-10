using System;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Impl.Sql.Translation.QueryEntities;
using Simple1C.Interface;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal class ValueLiteralRewriter : SqlVisitor
    {
        private readonly QueryEntityAccessor queryEntityAccessor;
        private readonly QueryEntityRegistry queryEntityRegistry;
        private static readonly byte[] emptyReference = new byte[16];

        public ValueLiteralRewriter(QueryEntityAccessor queryEntityAccessor,
            QueryEntityRegistry queryEntityRegistry)
        {
            this.queryEntityAccessor = queryEntityAccessor;
            this.queryEntityRegistry = queryEntityRegistry;
        }

        public override ISqlElement VisitValueLiteral(ValueLiteralExpression expression)
        {
            var result = GetSqlOrNull(expression.Value);
            if (result == null)
            {
                const string messageFormat = "operator [Значение] has invalid argument [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, expression.Value));
            }
            return result;
        }

        private ISqlElement GetSqlOrNull(string value)
        {
            var valueItems = value.Split('.');
            if (valueItems.Length < 3)
                return null;
            var name = ConfigurationName.ParseOrNull(valueItems[0] + "." + valueItems[1]);
            if (!name.HasValue)
                return null;
            var objectValue = valueItems[2];
            if (objectValue.EqualsIgnoringCase("ПустаяСсылка"))
                return new LiteralExpression
                {
                    Value = emptyReference
                };
            if (name.Value.Scope == ConfigurationScope.Перечисления)
                return GetEnumValueSql(name.Value.Fullname, objectValue);
            return null;
        }

        private ISqlElement GetEnumValueSql(string enumName, string enumValueName)
        {
            var table = queryEntityRegistry.CreateQueryEntity(null, enumName);
            var selectClause = new SelectClause {Source = queryEntityAccessor.GetTableDeclaration(table)};
            selectClause.Fields.Add(new SelectFieldExpression
            {
                Expression = new ColumnReferenceExpression
                {
                    Name = table.GetIdColumnName(),
                    Table = (TableDeclarationClause) selectClause.Source
                }
            });
            var enumMappingsJoinClause = queryEntityAccessor.CreateEnumMappingsJoinClause(table);
            selectClause.JoinClauses.Add(enumMappingsJoinClause);
            selectClause.WhereExpression = new EqualityExpression
            {
                Left = new ColumnReferenceExpression
                {
                    Name = "enumValueName",
                    Table = (TableDeclarationClause) enumMappingsJoinClause.Source
                },
                Right = new LiteralExpression
                {
                    Value = enumValueName
                }
            };
            return new SubqueryClause
            {
                Query = new SqlQuery
                {
                    Unions = {new UnionClause {SelectClause = selectClause}}
                }
            };
        }
    }
}