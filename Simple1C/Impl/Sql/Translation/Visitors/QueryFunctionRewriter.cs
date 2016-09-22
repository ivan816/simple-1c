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
            if (expression.Function == KnownQueryFunction.DateTime)
            {
                ExpectArgumentCount(expression, 3);
                var yearLiteral = expression.Arguments[0] as LiteralExpression;
                var monthLiteral = expression.Arguments[1] as LiteralExpression;
                var dayLiteral = expression.Arguments[2] as LiteralExpression;
                if (yearLiteral == null || monthLiteral == null || dayLiteral == null)
                {
                    var message = string.Format("Expected DateTime function parameter to be literals, " +
                                                "but was [{0}]", expression.Arguments.JoinStrings(","));
                    throw new InvalidOperationException(message);
                }
                return new CastExpression
                {
                    Type = "date",
                    Expression = new LiteralExpression
                    {
                        Value =
                            new DateTime((int) yearLiteral.Value,
                                (int) monthLiteral.Value,
                                (int) dayLiteral.Value)
                                .ToString("yyyy-MM-dd")
                    }
                };
            }
            if (expression.Function == KnownQueryFunction.Year)
            {
                ExpectArgumentCount(expression, 1);
                return new QueryFunctionExpression
                {
                    Function = KnownQueryFunction.SqlDatePart,
                    Arguments = new List<ISqlElement>
                    {
                        new LiteralExpression {Value = "year"},
                        Visit(expression.Arguments[0])
                    }
                };
            }
            if (expression.Function == KnownQueryFunction.Quarter)
            {
                ExpectArgumentCount(expression, 1);
                return new QueryFunctionExpression
                {
                    Function = KnownQueryFunction.SqlDatePart,
                    Arguments = new List<ISqlElement>
                    {
                        new LiteralExpression {Value = "quarter"},
                        Visit(expression.Arguments[0])
                    }
                };
            }
            if (expression.Function == KnownQueryFunction.Presentation)
            {
                ExpectArgumentCount(expression, 1);
                return Visit(expression.Arguments[0]);
            }
            if (expression.Function == KnownQueryFunction.IsNull)
            {
                ExpectArgumentCount(expression, 2);
                return new CaseExpression
                {
                    Elements =
                    {
                        new CaseElement
                        {
                            Condition = new IsNullExpression {Argument = Visit(expression.Arguments[0])},
                            Value = Visit(expression.Arguments[1])
                        }
                    },
                    DefaultValue = Visit(expression.Arguments[0])
                };
            }
            if (expression.Function == KnownQueryFunction.Substring)
            {
                ExpectArgumentCount(expression, 3);
                return new QueryFunctionExpression
                {
                    Function = KnownQueryFunction.Substring,
                    Arguments =
                    {
                        new CastExpression
                        {
                            Type = "varchar",
                            Expression = Visit(expression.Arguments[0])
                        },
                        Visit(expression.Arguments[1]),
                        Visit(expression.Arguments[2])
                    }
                };
            }
            if (expression.Function == KnownQueryFunction.SqlDateTrunc)
            {
                ExpectArgumentCount(expression, 2);
                return new QueryFunctionExpression
                {
                    Function = KnownQueryFunction.SqlDateTrunc,
                    Arguments = {Visit(expression.Arguments[1]), Visit(expression.Arguments[0])}
                };
            }
            return base.VisitQueryFunction(expression);
        }

        private static void ExpectArgumentCount(QueryFunctionExpression expression, int expectedCount)
        {
            if (expression.Arguments.Count != expectedCount)
            {
                var message = string.Format("{0} function expected exactly {1} arguments but was {2}",
                    expression.Function, expectedCount, expression.Arguments.Count);
                throw new InvalidOperationException(message);
            }
        }
    }
}