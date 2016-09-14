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

        public override UnionClause VisitUnion(UnionClause clause)
        {
            builder.Append("\r\n\r\nunion");
            if (clause.Type == UnionType.All)
                builder.Append(" all");
            builder.Append("\r\n\r\n");
            return base.VisitUnion(clause);
        }

        public override AggregateFunction VisitAggregateFunction(AggregateFunction expression)
        {
            builder.Append(FormatAggregateFunction(expression.Type));
            return expression;
        }

        public override GroupByClause VisitGroupBy(GroupByClause clause)
        {
            builder.Append("\r\ngroup by ");
            VisitEnumerable(clause.Columns, ",");
            return clause;
        }

        public override OrderByClause VisitOrderBy(OrderByClause element)
        {
            builder.Append("\r\norder by ");
            VisitEnumerable(element.Expressions, ",");
            return element;
        }

        public override ISqlElement VisitOrderingElement(OrderByClause.OrderingElement orderingElement)
        {
            Visit(orderingElement.Expression);
            builder.AppendFormat(" {0}", orderingElement.IsAsc ? "asc" : "desc");
           return orderingElement;
        }

        private static string FormatAggregateFunction(AggregateFunctionType f)
        {
            switch (f)
            {
                case AggregateFunctionType.Count:
                    return "count(*)";
                case AggregateFunctionType.Sum:
                    return "sum(*)";
                case AggregateFunctionType.Max:
                    return "max(*)";
                case AggregateFunctionType.Min:
                    return "min(*)";
                default:
                    throw new ArgumentOutOfRangeException("f", f, null);
            }
        }

        public override ISqlElement VisitSubquery(SubqueryClause clause)
        {
            builder.Append("(");
            Visit(clause.SelectClause);
            builder.Append(")");
            WriteAlias(clause.Alias);
            return clause;
        }

        public override SelectClause VisitSelect(SelectClause clause)
        {
            builder.Append("select\r\n\t");
            if (clause.IsSelectAll)
                builder.Append("*");
            else
                VisitEnumerable(clause.Fields, ",\r\n\t");
            builder.Append("\r\nfrom ");
            Visit(clause.Source);
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
            if (clause.GroupBy != null)
                Visit(clause.GroupBy);
            if (clause.Union != null)
                Visit(clause.Union);
            if (clause.OrderBy != null)
                Visit(clause.OrderBy);
            return clause;
        }

        public override SelectFieldElement VisitSelectField(SelectFieldElement clause)
        {
            Visit(clause.Expression);
            WriteAlias(clause.Alias);
            return clause;
        }

        public override CaseExpression VisitCase(CaseExpression expression)
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
            if (!string.IsNullOrEmpty(expression.Declaration.Alias))
            {
                builder.Append(expression.Declaration.Alias);
                builder.Append(".");
            }
            builder.Append(expression.Name);
            return expression;
        }

        public override ISqlElement VisitTableDeclaration(TableDeclarationClause clause)
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

        public override JoinClause VisitJoin(JoinClause clause)
        {
            builder.Append(GetJoinKindString(clause));
            builder.Append(" join ");
            Visit(clause.Source);
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

        public override ISqlElement VisitQueryFunction(QueryFunctionExpression expression)
        {
            builder.Append(FormatQueryFunctionName(expression.FunctionName));
            builder.Append('(');
            VisitEnumerable(expression.Arguments, ",");
            builder.Append(')');
            return expression;
        }

        private static string FormatQueryFunctionName(QueryFunctionName name)
        {
            switch (name)
            {
                case QueryFunctionName.SqlDatePart:
                    return "date_part";
                case QueryFunctionName.SqlDateTrunc:
                    return "date_trunc";
                case QueryFunctionName.SqlNot:
                    return "not";
                default:
                    const string messageFormat = "unexpected function [{0}]";
                    throw new InvalidOperationException(string.Format(messageFormat, name));
            }
        }

        public override ISqlElement VisitValueLiteral(ValueLiteralExpression expression)
        {
            NotSupported(expression, expression.ObjectName);
            return expression;
        }

        private static void NotSupported(ISqlElement element, params object[] args)
        {
            const string messageFormat = "element [{0}] can't be turned to sql, " +
                                         "must be rewritten to something else first";
            var argsString = args.JoinStrings(",");
            throw new InvalidOperationException(string.Format(messageFormat,
                element.GetType().FormatName() + (argsString == "" ? "" : ":" + argsString)));
        }

        private static string GetOperatorText(SqlBinaryOperator op)
        {
            switch (op)
            {
                case SqlBinaryOperator.Eq:
                    return " = ";
                case SqlBinaryOperator.Neq:
                    return " <> ";
                case SqlBinaryOperator.And:
                    return " and ";
                case SqlBinaryOperator.Or:
                    return " or ";
                case SqlBinaryOperator.LessThan:
                    return " < ";
                case SqlBinaryOperator.LessThanOrEqual:
                    return " <= ";
                case SqlBinaryOperator.GreaterThan:
                    return " > ";
                case SqlBinaryOperator.GreaterThanOrEqual:
                    return " >= ";
                case SqlBinaryOperator.Plus:
                    return " + ";
                case SqlBinaryOperator.Minus:
                    return " - ";
                case SqlBinaryOperator.Like:
                    return " like ";
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
            if (value is DateTime)
                return "cast('" + ((DateTime) value).ToString("yyyy-MM-dd") + "' as date)";
            if (value is bool)
                return ((bool?) value).Value ? "true" : "false";
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
                    throw new InvalidOperationException(string.Format(messageFormat, value,
                        value == null ? "<null>" : value.GetType().FormatName(), sqlType));
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