using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation
{
    internal class SqlFormatter : SqlVisitor
    {
        private ISqlElement parentOperatorExpression;
        private readonly StringBuilder builder = new StringBuilder();

        public static string Format(ISqlElement element)
        {
            var formatter = new SqlFormatter();
            formatter.Visit(element);
            return formatter.builder.ToString();
        }

        public override UnionClause VisitUnion(UnionClause clause)
        {
            var result = base.VisitUnion(clause);
            if (clause.Type.HasValue)
            {
                builder.Append("\r\n\r\nunion");
                if (clause.Type == UnionType.All)
                    builder.Append(" all");
                builder.Append("\r\n\r\n");
            }
            return result;
        }

        public override AggregateFunctionExpression VisitAggregateFunction(AggregateFunctionExpression expression)
        {
            builder.Append(expression.Function.ToString().ToLower());
            builder.Append("(");
            if (expression.IsSelectAll)
                builder.Append("*");
            else
                Visit(expression.Argument);
            builder.Append(")");
            return expression;
        }

        public override GroupByClause VisitGroupBy(GroupByClause clause)
        {
            builder.Append("\r\ngroup by ");
            VisitEnumerable(clause.Expressions, ",");
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

        public override SubqueryClause VisitSubquery(SubqueryClause clause)
        {
            var previous = parentOperatorExpression;
            parentOperatorExpression = null;
            builder.Append("(");
            Visit(clause.Query);
            builder.Append(")");
            parentOperatorExpression = previous;
            return clause;
        }

        public override SubqueryTable VisitSubqueryTable(SubqueryTable subqueryTable)
        {
            Visit(subqueryTable.Query);
            if (string.IsNullOrWhiteSpace(subqueryTable.Alias))
            {
                var message = string.Format("Subquery must have an alias but did not: [{0}]", subqueryTable);
                throw new InvalidOperationException(message);
            }
            builder.AppendFormat(" as {0}", subqueryTable.Alias);
            return subqueryTable;
        }

        public override SelectClause VisitSelect(SelectClause clause)
        {
            builder.Append("select\r\n\t");

            if (clause.IsDistinct)
                builder.AppendFormat(" distinct ");
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
            if (clause.Having != null)
            {
                builder.Append("\r\nhaving ");
                Visit(clause.Having);
            }
            if (clause.Top.HasValue)
                builder.AppendFormat("\r\nlimit {0}", clause.Top.Value);
            return clause;
        }

        public override SelectFieldExpression VisitSelectField(SelectFieldExpression clause)
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

        public override ISqlElement VisitUnary(UnaryExpression unaryExpression)
        {
            var needParens = NeedParens(unaryExpression);
            var previous = parentOperatorExpression;
            parentOperatorExpression = unaryExpression;
            if (needParens)
                builder.Append("(");
            builder.AppendFormat(" {0} ", GetOperatorText(unaryExpression.Operator));
            Visit(unaryExpression.Argument);
            if (needParens)
                builder.Append(")");
            parentOperatorExpression = previous;
            return unaryExpression;
        }

        public override ISqlElement VisitBinary(BinaryExpression expression)
        {
            var needParens = NeedParens(expression);
            var previous = parentOperatorExpression;
            parentOperatorExpression = expression;
            if (needParens)
                builder.Append("(");
            Visit(expression.Left);
            builder.AppendFormat(" {0} ", GetOperatorText(expression.Operator));
            Visit(expression.Right);
            if (needParens)
                builder.Append(")");
            parentOperatorExpression = previous;
            return expression;
        }

        public override ISqlElement VisitColumnReference(ColumnReferenceExpression expression)
        {
            var alias = expression.Table.Alias;
            if (!string.IsNullOrEmpty(alias))
            {
                builder.Append(alias);
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
            Visit(expression.Source);
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
            var functionName = expression.KnownFunction.HasValue
                ? FormatQueryFunctionName(expression.KnownFunction.Value)
                : expression.CustomFunction;
            builder.Append(functionName);
            builder.Append('(');
            VisitEnumerable(expression.Arguments, ", ");
            builder.Append(')');
            return expression;
        }

        public override ISqlElement VisitList(ListExpression listExpression)
        {
            builder.Append('(');
            VisitEnumerable(listExpression.Elements, ", ");
            builder.Append(')');
            return listExpression;
        }

        private static string FormatQueryFunctionName(KnownQueryFunction name)
        {
            switch (name)
            {
                case KnownQueryFunction.SqlDatePart:
                    return "date_part";
                case KnownQueryFunction.SqlDateTrunc:
                    return "date_trunc";
                case KnownQueryFunction.SqlNot:
                    return "not";
                case KnownQueryFunction.Substring:
                    return "substring";
                default:
                    throw new InvalidOperationException(string.Format("unexpected function [{0}]", name));
            }
        }

        public override ISqlElement VisitValueLiteral(ValueLiteralExpression expression)
        {
            NotSupported(expression, expression.ObjectName);
            return expression;
        }

        public override ISqlElement VisitIsNullExpression(IsNullExpression expression)
        {
            Visit(expression.Argument);
            builder.Append(" is ");
            if (expression.IsNotNull)
                builder.Append("not ");
            builder.Append("null");
            return expression;
        }

        public override ISqlElement VisitCast(CastExpression castExpression)
        {
            builder.Append("cast(");
            Visit(castExpression.Expression);
            builder.AppendFormat(" as {0})", castExpression.Type);
            return castExpression;
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
                    return "=";
                case SqlBinaryOperator.Neq:
                    return "<>";
                case SqlBinaryOperator.And:
                    return "and";
                case SqlBinaryOperator.Or:
                    return "or";
                case SqlBinaryOperator.LessThan:
                    return "<";
                case SqlBinaryOperator.LessThanOrEqual:
                    return "<=";
                case SqlBinaryOperator.GreaterThan:
                    return ">";
                case SqlBinaryOperator.GreaterThanOrEqual:
                    return ">=";
                case SqlBinaryOperator.Plus:
                    return "+";
                case SqlBinaryOperator.Minus:
                    return "-";
                case SqlBinaryOperator.Mult:
                    return "*";
                case SqlBinaryOperator.Div:
                    return "/";
                case SqlBinaryOperator.Remainder:
                    return "%";
                case SqlBinaryOperator.Like:
                    return "like";
                default:
                    throw new ArgumentOutOfRangeException("op", op, null);
            }
        }

        private static string GetOperatorText(UnaryOperator op)
        {
            switch (op)
            {
                case UnaryOperator.Not:
                    return "not";
                default:
                    const string messageFormat = "unexpected operator type [{0}]";
                    throw new InvalidOperationException(string.Format(messageFormat, op));
            }
        }

        private static string FormatValueAsString(object value)
        {
            var str = value as string;
            if (str != null)
                return "'" + str.Replace("\'", "\'\'") + "'";
            var bytes = value as byte[];
            if (bytes != null)
                return "E'\\\\x" + bytes.ToHex() + "'";
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
                case SqlType.DatePart:
                    return value.ToString();
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
                case JoinKind.Full:
                    return "full outer";
                default:
                    throw new InvalidOperationException(string.Format("unexpected join kind [{0}]", joinClause.JoinKind));
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

        private bool NeedParens(ISqlElement current)
        {
            return GetOperatorPrecedence(parentOperatorExpression) > GetOperatorPrecedence(current);
        }

        private static int? GetOperatorPrecedence(ISqlElement element)
        {
            var binaryExpression = element as BinaryExpression;
            if (binaryExpression != null)
                return GetPrecedence(binaryExpression.Operator);

            var unaryExpression = element as UnaryExpression;
            if (unaryExpression != null)
                return GetPrecedence(unaryExpression.Operator);
            return null;
        }

        private static int GetPrecedence<TOp>(TOp op) 
            where TOp : struct
        {
            var attribute = EnumAttributesCache<OperatorPrecedenceAttribute>.GetAttribute(op);
            return attribute.Precedence;
        }
    }
}