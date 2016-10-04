using System;
using System.Collections.Generic;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal class QueryFunctionRewriter : SqlVisitor
    {
        public override ISqlElement VisitQueryFunction(QueryFunctionExpression expression)
        {
            expression = (QueryFunctionExpression) base.VisitQueryFunction(expression);
            if (expression.KnownFunction == KnownQueryFunction.DateTime)
            {
                ExpectArgumentCount(expression, 3);
                var yearLiteral = expression.Arguments[0] as LiteralExpression;
                var monthLiteral = expression.Arguments[1] as LiteralExpression;
                var dayLiteral = expression.Arguments[2] as LiteralExpression;
                if (yearLiteral == null || monthLiteral == null || dayLiteral == null)
                {
                    const string messageFormat = "expected DateTime function parameters " +
                                                 "to be literals, but was [{0}]";
                    throw new InvalidOperationException(string.Format(messageFormat,
                        expression.Arguments.JoinStrings(",")));
                }
                return new CastExpression
                {
                    Type = "date",
                    Expression = new LiteralExpression
                    {
                        Value = new DateTime((int) yearLiteral.Value,
                            (int) monthLiteral.Value,
                            (int) dayLiteral.Value)
                            .ToString("yyyy-MM-dd")
                    }
                };
            }
            if (expression.KnownFunction == KnownQueryFunction.Year)
            {
                ExpectArgumentCount(expression, 1);
                return new QueryFunctionExpression
                {
                    KnownFunction = KnownQueryFunction.SqlDatePart,
                    Arguments = new List<ISqlElement>
                    {
                        new LiteralExpression {Value = "year"},
                        expression.Arguments[0]
                    }
                };
            }
            if (expression.KnownFunction == KnownQueryFunction.Quarter)
            {
                ExpectArgumentCount(expression, 1);
                return new QueryFunctionExpression
                {
                    KnownFunction = KnownQueryFunction.SqlDatePart,
                    Arguments = new List<ISqlElement>
                    {
                        new LiteralExpression {Value = "quarter"},
                        expression.Arguments[0]
                    }
                };
            }
            if (expression.KnownFunction == KnownQueryFunction.Presentation)
            {
                ExpectArgumentCount(expression, 1);
                return expression.Arguments[0];
            }
            if (expression.KnownFunction == KnownQueryFunction.IsNull)
            {
                ExpectArgumentCount(expression, 2);
                return new CaseExpression
                {
                    Elements =
                    {
                        new CaseElement
                        {
                            Condition = new IsNullExpression {Argument = expression.Arguments[0]},
                            Value = expression.Arguments[1]
                        }
                    },
                    DefaultValue = expression.Arguments[0]
                };
            }
            if (expression.KnownFunction == KnownQueryFunction.Substring)
            {
                ExpectArgumentCount(expression, 3);
                return new QueryFunctionExpression
                {
                    KnownFunction = KnownQueryFunction.Substring,
                    Arguments =
                    {
                        new CastExpression
                        {
                            Type = "varchar",
                            Expression = expression.Arguments[0]
                        },
                        expression.Arguments[1],
                        expression.Arguments[2]
                    }
                };
            }
            if (expression.KnownFunction == KnownQueryFunction.SqlDateTrunc)
            {
                ExpectArgumentCount(expression, 2);
                return new QueryFunctionExpression
                {
                    KnownFunction = KnownQueryFunction.SqlDateTrunc,
                    Arguments = {expression.Arguments[1], expression.Arguments[0]}
                };
            }
            return expression;
        }

        private static void ExpectArgumentCount(QueryFunctionExpression expression, int expectedCount)
        {
            if (expression.Arguments.Count != expectedCount)
            {
                var message = string.Format("{0} function expected exactly {1} arguments but was {2}",
                    expression.KnownFunction, expectedCount, expression.Arguments.Count);
                throw new InvalidOperationException(message);
            }
        }
    }
}