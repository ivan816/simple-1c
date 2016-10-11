using System;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Impl.Sql.Translation.QueryEntities;
using Simple1C.Interface;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal class ValueLiteralRewriter : SqlVisitor
    {
        private readonly EnumSqlBuilder enumSqlBuilder;
        private static readonly byte[] emptyReference = new byte[16];

        public ValueLiteralRewriter(EnumSqlBuilder enumSqlBuilder)
        {
            this.enumSqlBuilder = enumSqlBuilder;
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
                return enumSqlBuilder.GetValueSql(name.Value.Fullname, objectValue);
            return null;
        }
    }
}