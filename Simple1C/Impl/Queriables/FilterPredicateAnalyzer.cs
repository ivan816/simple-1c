using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;
using Simple1C.Impl.Helpers;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Impl.Queriables
{
    internal class FilterPredicateAnalyzer : RelinqExpressionVisitor
    {
        private readonly QueryBuilder queryBuilder;
        private readonly MemberAccessBuilder memberAccessBuilder;
        private readonly StringBuilder filterBuilder = new StringBuilder();
        private readonly Dictionary<Expression, Expression> binaryExpressionMappings = new Dictionary<Expression, Expression>();
        private Expression xFilter;

        public FilterPredicateAnalyzer(QueryBuilder queryBuilder, MemberAccessBuilder memberAccessBuilder)
        {
            this.queryBuilder = queryBuilder;
            this.memberAccessBuilder = memberAccessBuilder;
        }

        public void Apply(Expression xTargetFilter)
        {
            xFilter = xTargetFilter;
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

        public override Expression Visit(Expression node)
        {
            if (!IsValid(node))
            {
                var message = GetInvalidFilterErrorMessage();
                throw new InvalidOperationException(message);
            }
            return base.Visit(node);
        }

        private static bool IsValid(Expression node)
        {
            var isValid = node is MemberExpression ||
                          node is ConstantExpression ||
                          node is QuerySourceReferenceExpression ||
                          node.NodeType == ExpressionType.Convert ||
                          node.NodeType == ExpressionType.TypeIs ||
                          node.NodeType == ExpressionType.Not ||
                          node is BinaryExpression;
            if (isValid)
                return true;
            var xMethodCall = node as MethodCallExpression;
            if (xMethodCall != null)
            {
                if (xMethodCall.Method.DeclaringType == typeof(object) &&
                    xMethodCall.Method.Name == "GetType")
                    return true;
                if (IsLikeFunction(xMethodCall))
                    return true;
            }
            return false;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            EmitField(node);
            return node;
        }

        protected override Expression VisitQuerySourceReference(QuerySourceReferenceExpression node)
        {
            EmitField(node);
            return node;
        }

        private void EmitField(Expression node)
        {
            var queryField = memberAccessBuilder.GetFieldOrNull(node);
            if (queryField == null)
            {
                var message = GetInvalidFilterErrorMessage();
                throw new InvalidOperationException(message);
            }
            filterBuilder.Append(queryField.Expression);
            var comparand = binaryExpressionMappings.GetOrDefault(node) as ConstantExpression;
            var needReferenceKeyword = typeof(Abstract1CEntity).IsAssignableFrom(node.Type) &&
                                       comparand != null &&
                                       comparand.Value != null;
            if (needReferenceKeyword)
                filterBuilder.Append(".Ссылка");
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var value = node.Value;
            var type = node.Type;
            var comparand = binaryExpressionMappings.GetOrDefault(node);
            if (comparand != null)
            {
                if (type == typeof(object) && comparand.Type != typeof(object))
                    type = comparand.Type;
                if (comparand.NodeType == ExpressionType.Convert)
                    type = ((UnaryExpression)comparand).Operand.Type;    
            }
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (value == null)
            {
                var name = ConfigurationName.GetOrNull(type);
                if (name == null)
                    filterBuilder.Append("NULL");
                else
                    filterBuilder.AppendFormat("ЗНАЧЕНИЕ({0}.ПустаяСсылка)", name.Value.Fullname);
            }
            else if (value.GetType().FullName == "System.RuntimeType")
            {
                var valueType = (Type)value;
                filterBuilder.Append("ТИП(");
                filterBuilder.Append(Get1CTypeName(valueType));
                filterBuilder.Append(")");
            }
            else
            {
                var xMember = comparand as MemberExpression;
                if (value is Guid && xMember != null && xMember.Member.Name == EntityHelpers.idPropertyName)
                    value = new ConvertUniqueIdentifierCmd
                    {
                        entityType =
                            xMember.Member.DeclaringType == queryBuilder.QueryType
                                ? queryBuilder.SourceType
                                : xMember.Member.DeclaringType,
                        id = (Guid) value
                    };
                else if (type.IsEnum)
                    value = new ConvertEnumCmd {enumType = type, valueIndex = (int) value};
                else if (value is Abstract1CEntity)
                    value = ((Abstract1CEntity) value).Controller.ValueSource.GetBackingStorage();
                var parameterName = queryBuilder.AddParameter(value);
                filterBuilder.Append('&');
                filterBuilder.Append(parameterName);
            }
            return node;
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
            var methodName = node.Method.Name;
            if (node.Method.DeclaringType == typeof(object) && methodName == "GetType")
            {
                filterBuilder.Append("ТИПЗНАЧЕНИЯ(");
                Visit(node.Object);
                filterBuilder.Append(")");
                return node;
            }
            if (IsLikeFunction(node))
            {
                filterBuilder.Append("(");
                Visit(node.Object);
                filterBuilder.Append(" ПОДОБНО ");
                if (methodName == "Contains" || methodName == "EndsWith")
                    filterBuilder.Append("\"%\" + ");
                Visit(node.Arguments[0]);
                if (methodName == "Contains" || methodName == "StartsWith")
                    filterBuilder.Append(" + \"%\"");
                filterBuilder.Append(")");
                return node;
            }
            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            filterBuilder.Append("(");
            binaryExpressionMappings.Add(expression.Left, expression.Right);
            binaryExpressionMappings.Add(expression.Right, expression.Left);
            Visit(expression.Left);
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
            Visit(expression.Right);
            filterBuilder.Append(")");
            return expression;
        }

        private string GetInvalidFilterErrorMessage()
        {
            const string messageFormat = "can't apply 'Where' operator for expression [{0}]." +
                                         "Expression must be a chain of member accesses.";
            return string.Format(messageFormat, xFilter);
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

        private static bool IsLikeFunction(MethodCallExpression xMethodCall)
        {
            return (xMethodCall.Method.DeclaringType == typeof(string)) &&
                   ((xMethodCall.Method.Name == "Contains") ||
                    (xMethodCall.Method.Name == "EndsWith") ||
                    (xMethodCall.Method.Name == "StartsWith"));
        }
    }
}