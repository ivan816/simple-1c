using System;
using System.Linq.Expressions;
using Remotion.Linq.Parsing;

namespace Simple1C.Impl.Queriables
{
    internal class ParameterizingExpressionVisitor : RelinqExpressionVisitor
    {
        private int parameterIndex;
        private ParameterExpression xParameter;

        public Expression Parameterize(Expression expression, ParameterExpression xTargetParameter)
        {
            parameterIndex = 0;
            xParameter = xTargetParameter;
            return Visit(expression);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            return EmitParameterAccess(node.Type);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            return EmitParameterAccess(node.Type);
        }

        private Expression EmitParameterAccess(Type type)
        {
            var xArrayItem = Expression.ArrayIndex(xParameter, Expression.Constant(parameterIndex));
            var result = Expression.Convert(xArrayItem, type);
            parameterIndex++;
            return result;
        }
    }
}