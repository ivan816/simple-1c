using System;
using System.Collections.Generic;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation
{
    internal class QueryFunctionRewriter : SingleSelectSqlVisitorBase
    {
        public override ISqlElement VisitQueryFunction(QueryFunctionExpression expression)
        {
            if (expression.FunctionName == QueryFunctionName.DateTime)
            {
                if (expression.Arguments.Count != 3)
                    throw new InvalidOperationException("invalid function");
                var yearLiteral = expression.Arguments[0] as LiteralExpression;
                var monthLiteral = expression.Arguments[1] as LiteralExpression;
                var dayLiteral = expression.Arguments[2] as LiteralExpression;
                if (yearLiteral == null || monthLiteral == null || dayLiteral == null)
                    throw new InvalidOperationException("invalid function");
                return new LiteralExpression
                {
                    Value = new DateTime((int) yearLiteral.Value,
                        (int) monthLiteral.Value,
                        (int) dayLiteral.Value)
                };
            }
            if (expression.FunctionName == QueryFunctionName.Year)
            {
                if (expression.Arguments.Count != 1)
                    throw new InvalidOperationException("invalid function");
                return new QueryFunctionExpression
                {
                    FunctionName = QueryFunctionName.SqlDatePart,
                    Arguments = new List<ISqlElement>
                    {
                        new LiteralExpression {Value = "year"},
                        expression.Arguments[0]
                    }
                };
            }
            if (expression.FunctionName == QueryFunctionName.Quarter)
            {
                if (expression.Arguments.Count != 1)
                    throw new InvalidOperationException("invalid function");
                return new QueryFunctionExpression
                {
                    FunctionName = QueryFunctionName.SqlDateTrunc,
                    Arguments = new List<ISqlElement>
                    {
                        new LiteralExpression {Value = "quarter"},
                        expression.Arguments[0]
                    }
                };
            }
            if (expression.FunctionName == QueryFunctionName.Presentation)
            {
                if (expression.Arguments.Count != 1)
                    throw new InvalidOperationException("invalid function");
                return expression.Arguments[0];
            }
            return base.VisitQueryFunction(expression);
        }
    }
}