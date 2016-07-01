using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Remotion.Linq.Parsing;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Queriables
{
    internal class FilterPredicateAnalyzer : RelinqExpressionVisitor
    {
        private readonly QueryBuilder queryBuilder;
        private readonly MemberAccessBuilder memberAccessBuilder;
        private readonly StringBuilder filterBuilder = new StringBuilder();
        private readonly Dictionary<Expression, Type> typeMappings = new Dictionary<Expression, Type>();

        public FilterPredicateAnalyzer(QueryBuilder queryBuilder, MemberAccessBuilder memberAccessBuilder)
        {
            this.queryBuilder = queryBuilder;
            this.memberAccessBuilder = memberAccessBuilder;
        }

        public void Apply(Expression xFilter)
        {
            filterBuilder.Clear();
            Visit(xFilter);
            queryBuilder.AddWherePart(filterBuilder.ToString());
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
            {
                filterBuilder.Append("(НЕ ");
                base.VisitUnary(node);
                filterBuilder.Append(")");
                return node;
            }
            return base.VisitUnary(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var queryField = memberAccessBuilder.GetMembers(node);
            filterBuilder.Append(queryField.Expression);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression expression)
        {
            if (expression.Value == null)
            {
                var type = expression.Type;
                if (type == typeof(object))
                    typeMappings.TryGetValue(expression, out type);
                if (type != null)
                    type = Nullable.GetUnderlyingType(type) ?? type;
                var name = ConfigurationName.GetOrNull(type);
                if (name == null)
                    filterBuilder.Append("NULL");
                else
                    filterBuilder.AppendFormat("ЗНАЧЕНИЕ({0}.ПустаяСсылка)", name.Value.Fullname);
            }
            else if (expression.Value.GetType().FullName == "System.RuntimeType")
            {
                var valueType = (Type) expression.Value;
                filterBuilder.Append("ТИП(");
                filterBuilder.Append(Get1CTypeName(valueType));
                filterBuilder.Append(")");
            }
            else
                EmitParameterReference(expression.Value, typeMappings.GetOrDefault(expression));
            return expression;
        }

        private void EmitParameterReference(object value, Type type)
        {
            if (value is Guid && type != null)
                value = new ConvertUniqueIdentifierCmd {entityType = type, id = (Guid) value};
            else if (value.GetType().IsEnum)
                value = new ConvertEnumCmd {value = value};
            var parameterName = queryBuilder.AddParameter(value);
            filterBuilder.Append('&');
            filterBuilder.Append(parameterName);
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            if (node.NodeType == ExpressionType.TypeIs)
            {
                filterBuilder.Append("(");
                filterBuilder.Append("ТИПЗНАЧЕНИЯ(");
                Visit(node.Expression);
                filterBuilder.Append(") = ТИП(");
                filterBuilder.Append(Get1CTypeName(node.TypeOperand));
                filterBuilder.Append(")");
                filterBuilder.Append(")");
                return node;
            }
            return base.VisitTypeBinary(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(object) && node.Method.Name == "GetType")
            {
                filterBuilder.Append("ТИПЗНАЧЕНИЯ(");
                Visit(node.Object);
                filterBuilder.Append(")");
                return node;
            }
            return base.VisitMethodCall(node);
        }

        private void RegisterTypeMappingForUniqueIdentifier(Expression left, Expression right)
        {
            if (left.NodeType != ExpressionType.MemberAccess)
                return;
            if (right.NodeType != ExpressionType.Constant)
                return;
            if (right.Type != typeof(Guid?))
                return;
            var xMember = (MemberExpression) left;
            if (xMember.Member.Name != "УникальныйИдентификатор")
                return;
            if (ConfigurationName.GetOrNull(xMember.Expression.Type) == null)
                return;
            typeMappings.Add(right, xMember.Expression.Type);
        }

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            filterBuilder.Append("(");
            var left = expression.Left;
            var right = expression.Right;
            if (right.NodeType == ExpressionType.Convert && left.NodeType == ExpressionType.Constant)
            {
                left = RestoreEnumConstant(left, right);
                right = ((UnaryExpression) right).Operand;
            }
            else if (left.NodeType == ExpressionType.Convert && right.NodeType == ExpressionType.Constant)
            {
                right = RestoreEnumConstant(right, left);
                left = ((UnaryExpression) left).Operand;
            }
            if (left.Type == typeof(object) && right.Type != typeof(object))
                typeMappings.Add(left, right.Type);
            else if (right.Type == typeof(object) && left.Type != typeof(object))
                typeMappings.Add(right, left.Type);
            RegisterTypeMappingForUniqueIdentifier(left, right);
            RegisterTypeMappingForUniqueIdentifier(right, left);
            Visit(left);
            switch (expression.NodeType)
            {
                case ExpressionType.Equal:
                    filterBuilder.Append(" = ");
                    break;

                case ExpressionType.GreaterThan:
                    filterBuilder.Append(" > ");
                    break;

                case ExpressionType.GreaterThanOrEqual:
                    filterBuilder.Append(" >= ");
                    break;

                case ExpressionType.LessThan:
                    filterBuilder.Append(" < ");
                    break;

                case ExpressionType.LessThanOrEqual:
                    filterBuilder.Append(" <= ");
                    break;

                case ExpressionType.NotEqual:
                    filterBuilder.Append(" <> ");
                    break;

                case ExpressionType.AndAlso:
                case ExpressionType.And:
                    filterBuilder.Append(" И ");
                    break;

                case ExpressionType.OrElse:
                case ExpressionType.Or:
                    filterBuilder.Append(" ИЛИ ");
                    break;

                case ExpressionType.Add:
                    filterBuilder.Append(" + ");
                    break;

                case ExpressionType.Subtract:
                    filterBuilder.Append(" - ");
                    break;

                case ExpressionType.Multiply:
                    filterBuilder.Append(" * ");
                    break;

                case ExpressionType.Divide:
                    filterBuilder.Append(" / ");
                    break;

                default:
                    const string messageFormat = "unsupported operator [{0}]";
                    throw new InvalidOperationException(string.Format(messageFormat, expression.NodeType));
            }
            Visit(right);
            filterBuilder.Append(")");
            return expression;
        }

        private static Expression RestoreEnumConstant(Expression left, Expression right)
        {
            var enumValue = ((ConstantExpression) left).Value;
            var operandType = ((UnaryExpression) right).Operand.Type;
            Type enumType = null;
            if (operandType.IsEnum)
                enumType = operandType;
            else if (IsNullable(operandType) && operandType.GetGenericArguments()[0].IsEnum)
                enumType = operandType.GetGenericArguments()[0];
            if (enumValue == null)
                left = Expression.Constant(null, typeof(object));
            //typeof (Nullable<>).MakeGenericType(enumType));
            else if (enumType != null)
                left = Expression.Constant(Enum.GetValues(enumType)
                    .Cast<object>()
                    .Where((x, i) => i == (int) enumValue)
                    .Single(), operandType);
            return left;
        }

        private static bool IsNullable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static string Get1CTypeName(Type type)
        {
            if (type == typeof(string))
                return "СТРОКА";
            if (type == typeof(bool))
                return "БУЛЕВО";
            if (type == typeof(int) || type == typeof(uint) || type == typeof(byte) || type == typeof(long) ||
                type == typeof(ulong) || type == typeof(double) || type == typeof(float) || type == typeof(decimal))
                return "ЧИСЛО";
            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return "ДАТА";
            var name = ConfigurationName.GetOrNull(type);
            if (name.HasValue)
                return name.Value.Fullname;
            const string messageFormat = "can't detect 1C type for [{0}]";
            throw new InvalidOperationException(string.Format(messageFormat, type.Name));
        }
    }
}