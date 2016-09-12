using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.SqlAccess
{
    internal class SqlFormatter : SqlVisitor
    {
        private readonly StringBuilder builder = new StringBuilder();

        public static string Format(ISqlElement element)
        {
            var formatter = new SqlFormatter();
            formatter.Visit(element);
            return formatter.builder.ToString();
        }

        public override ISqlElement VisitSelect(SelectClause clause)
        {
            builder.Append("(select\r\n\t");
            VisitEnumerable(clause.Columns, ",\r\n\t");
            builder.Append("\r\nfrom ");
            Visit(clause.Table);
            if (clause.JoinClauses.Count > 0)
            {
                builder.Append("\r\n");
                VisitEnumerable(clause.JoinClauses, "\r\n");
            }
            if (clause.WhereExpression != null)
            {
                builder.Append("\r\nwhere ");
                Visit(clause.WhereExpression);
            }
            builder.Append(")");
            return clause;
        }

        public override ISqlElement VisitSelectColumn(SelectColumn clause)
        {
            Visit(clause.Expression);
            WriteAlias(clause.Alias);
            return clause;
        }

        public override ISqlElement VisitCase(CaseExpression expression)
        {
            builder.Append("case");
            foreach (var e in expression.Elements)
            {
                builder.Append("\r\n\t");
                builder.Append("when ");
                Visit(e.Condition);
                builder.Append(" then ");
                Visit(e.Value);
            }
            if (expression.DefaultValue != null)
            {
                builder.Append("\r\n\t");
                builder.Append("else ");
                Visit(expression.DefaultValue);
            }
            builder.Append("\r\nend");
            return expression;
        }

        public override ISqlElement VisitBinary(BinaryExpression expression)
        {
            Visit(expression.Left);
            builder.Append(GetOperatorText(expression.Op));
            Visit(expression.Right);
            return expression;
        }

        public override ISqlElement VisitColumnReference(ColumnReferenceExpression expression)
        {
            builder.Append(expression.TableName);
            builder.Append(".");
            builder.Append(expression.Name);
            return expression;
        }

        public override ISqlElement VisitDeclaration(DeclarationClause clause)
        {
            builder.Append(clause.Name);
            WriteAlias(clause.Alias);
            return clause;
        }

        public override ISqlElement VisitIn(InExpression expression)
        {
            Visit(expression.Column);
            builder.Append(" in ");
            builder.Append('(');
            VisitEnumerable(expression.Values, ",");
            builder.Append(')');
            return expression;
        }

        public override ISqlElement VisitJoin(JoinClause clause)
        {
            builder.Append(GetJoinKindString(clause));
            builder.Append(" join ");
            Visit(clause.Table);
            builder.Append(" on ");
            Visit(clause.Condition);
            return clause;
        }

        public override ISqlElement VisitLiteral(LiteralExpression expression)
        {
            var value = expression.SqlType.HasValue
                ? ApplySqlType(expression.Value, expression.SqlType.Value)
                : expression.Value;
            builder.Append(FormatValueAsString(value));
            return expression;
        }

        public override ISqlElement VisitUnary(UnaryFunctionExpression expression)
        {
            builder.Append(expression.FunctionName);
            builder.Append('(');
            Visit(expression.Argument);
            builder.Append(')');
            return expression;
        }

        private static string GetOperatorText(SqlBinaryOperator op)
        {
            switch (op)
            {
                case SqlBinaryOperator.Eq:
                    return " = ";
                case SqlBinaryOperator.And:
                    return " and ";
                case SqlBinaryOperator.Or:
                    return " or ";
                default:
                    throw new ArgumentOutOfRangeException("op", op, null);
            }
        }

        private static string FormatValueAsString(object value)
        {
            if (value is string)
                return "'" + value + "'";
            if (value is byte[])
                return "E'\\\\x" + ((byte[]) value).ToHex() + "'";
            return value.ToString();
        }

        private static object ApplySqlType(object value, SqlType sqlType)
        {
            switch (sqlType)
            {
                case SqlType.ByteArray:
                    var b = value as byte?;
                    if (b.HasValue)
                        return new[] {b.Value};
                    var i = value as int?;
                    if (i.HasValue)
                        return BitConverter.GetBytes(i.Value).Reverse().ToArray();
                    const string messageFormat = "can't convert value [{0}] of type [{1}] to [{2}]";
                    throw new InvalidOperationException(string.Format(messageFormat,
                        value, value == null ? "<null>" : value.GetType().FormatName(),
                        sqlType));
                default:
                    const string message = "unexpected value [{0}] of SqlType";
                    throw new InvalidOperationException(string.Format(message, sqlType));
            }
        }

        private static string GetJoinKindString(JoinClause joinClause)
        {
            switch (joinClause.JoinKind)
            {
                case JoinKind.Left:
                    return "left";
                case JoinKind.Right:
                    return "right";
                case JoinKind.Inner:
                    return "inner";
                case JoinKind.Outer:
                    return "outer";
                default:
                    const string messageFormat = "unexpected join kind [{0}]";
                    throw new InvalidOperationException(string.Format(messageFormat, joinClause.JoinKind));
            }
        }

        private void WriteAlias(string alias)
        {
            if (!string.IsNullOrEmpty(alias))
            {
                builder.Append(" as ");
                builder.Append(alias);
            }
        }

        private void VisitEnumerable(IEnumerable<ISqlElement> elements, string delimiter)
        {
            var isFirst = true;
            foreach (var e in elements)
            {
                if (isFirst)
                    isFirst = false;
                else
                    builder.Append(delimiter);
                Visit(e);
            }
        }
    }
}