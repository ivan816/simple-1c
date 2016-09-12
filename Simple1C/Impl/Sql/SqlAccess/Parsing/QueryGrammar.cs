using System;
using System.Linq;
using Irony.Parsing;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    public class QueryGrammar : Grammar
    {
        public QueryGrammar()
            : base(false)
        {
            LanguageFlags = LanguageFlags.CreateAst;

            //terminals
            var termComma = ToTerm(",");
            var dot = ToTerm(".");
            var termSelect = ToTerm("SELECT");
            var termFrom = ToTerm("FROM");
            var termAs = ToTerm("AS");
            var termCount = ToTerm("COUNT");
            var termJoin = ToTerm("JOIN");
            var termOn = ToTerm("ON");
            var idSimple = new IdentifierTerminal("Identifier");
            idSimple.SetFlag(TermFlags.NoAstNode);

            //nonterminals
            var id = NonTerminal("id", null, n => new Identifier
            {
                Value = n.ChildNodes.Select(x => x.Token.ValueString).JoinStrings(".")
            });
            id.Rule = MakePlusRule(id, dot, idSimple);

            var aggregateName = NonTerminal("aggregateName", termCount | "Min" | "Max" | "Sum");

            var aggregateArg = NonTerminal("aggregateArg", "*");

            var aggregate = NonTerminal("aggregate",
                aggregateName + "(" + aggregateArg + ")",
                delegate(ParseTreeNode node)
                {
                    var aggregateFunctionNameString = node.ChildNodes[0].ChildNodes[0].Token.ValueString;
                    AggregateFunctionType aggregateFunctionType;
                    switch (aggregateFunctionNameString.ToLower())
                    {
                        case "count":
                            aggregateFunctionType = AggregateFunctionType.Count;
                            break;
                        case "min":
                            aggregateFunctionType = AggregateFunctionType.Min;
                            break;
                        case "max":
                            aggregateFunctionType = AggregateFunctionType.Max;
                            break;
                        case "sum":
                            aggregateFunctionType = AggregateFunctionType.Sum;
                            break;
                        default:
                            const string messageFormat = "unexpected aggregate function [{0}]";
                            throw new InvalidOperationException(string.Format(messageFormat, aggregateFunctionNameString));
                    }
                    return new AggregateFunction {Type = aggregateFunctionType};
                });

            var columnSource = NonTerminal("columnSource", id | aggregate);

            var asOpt = NonTerminal("asOpt", Empty | termAs);
            var aliasOpt = NonTerminal("aliasOpt", Empty | asOpt + id);

            var columnItem = NonTerminal("columnItem", columnSource + aliasOpt, n =>
            {
                var ids = n.Elements().OfType<Identifier>().ToArray();
                var aggregateFunction = n.Elements().OfType<AggregateFunction>().SingleOrDefault();
                return new SelectColumn
                {
                    Expression = (ISqlElement) aggregateFunction
                                 ?? new ColumnReferenceExpression
                                 {
                                     Name = ids[0].Value
                                 },
                    Alias = ids.Length > 1 ? ids[1].Value : null
                };
            });

            var columnItemList = NonTerminal("columnItemList", null);
            columnItemList.Rule = MakePlusRule(columnItemList, termComma, columnItem);

            var selList = NonTerminal("selList", columnItemList | "*");

            var number = new NumberLiteral("number", NumberOptions.Default,
                (context, node) => node.AstNode = new LiteralExpression
                {
                    Value = node.Token.Value
                });
            var term = NonTerminal("term", id | number, delegate(ParseTreeNode node)
            {
                var result = node.ChildNodes[0].AstNode;
                var identifier = result as Identifier;
                if(identifier != null)
                    result = new ColumnReferenceExpression
                    {
                        Name = identifier.Value
                    };
                return result;
            });

            var binOp = NonTerminal("binOp",
                ToTerm("+") | "-" | "=" | ">" | "<" | ">=" | "<=" | "<>" | "!="
                | "AND" | "OR", delegate(ParseTreeNode node)
                {
                    var operatorText = node.ChildNodes[0].Token.ValueString;
                    switch (operatorText)
                    {
                        case "+":
                            return SqlBinaryOperator.Plus;
                        case "-":
                            return SqlBinaryOperator.Minus;
                        case "=":
                            return SqlBinaryOperator.Eq;
                        case ">":
                            return SqlBinaryOperator.GreaterThan;
                        case "<":
                            return SqlBinaryOperator.LessThan;
                        case ">=":
                            return SqlBinaryOperator.GreaterThanOrEqual;
                        case "<=":
                            return SqlBinaryOperator.LessThanOrEqual;
                        case "<>":
                        case "!=":
                            return SqlBinaryOperator.Neq;
                        default:
                            const string messageFormat = "unexpected operator [{0}]";
                            throw new InvalidOperationException(string.Format(messageFormat, operatorText));
                    }
                });
            binOp.SetFlag(TermFlags.InheritPrecedence);

            var binExpr = NonTerminal("binExpr", null, delegate(ParseTreeNode node)
            {
                var left = (ISqlElement) node.ChildNodes[0].AstNode;
                var binaryOperator = (SqlBinaryOperator) node.ChildNodes[1].AstNode;
                var right = (ISqlElement) node.ChildNodes[2].AstNode;
                if (binaryOperator == SqlBinaryOperator.And)
                    return new AndExpression
                    {
                        Left = left,
                        Right = right
                    };
                if (binaryOperator == SqlBinaryOperator.Eq)
                    return new EqualityExpression
                    {
                        Left = left,
                        Right = right
                    };
                return new BinaryExpression(binaryOperator)
                {
                    Left = left,
                    Right = right
                };
            });
            var expression = NonTerminal("expression",
                term | binExpr,
                n => n.ChildNodes[0].AstNode);
            expression.SetFlag(TermFlags.IsTransient);
            binExpr.Rule = expression + binOp + expression;

            var declaration = NonTerminal("declaration",
                id + aliasOpt,
                delegate(ParseTreeNode n)
                {
                    return new DeclarationClause
                    {
                        Name = ((Identifier) n.ChildNodes[0].AstNode).Value,
                        Alias = n.ChildNodes[1].Elements().OfType<Identifier>().Select(x => x.Value).SingleOrDefault()
                    };
                });

            var joinKindOpt = NonTerminal("joinKindOpt", Empty | "OUTER" | "INNER" | "LEFT" | "RIGHT");

            var joinItem = NonTerminal("joinItem",
                joinKindOpt + termJoin + declaration + termOn + expression,
                delegate(ParseTreeNode node)
                {
                    var joinKindNodes = node.ChildNodes[0];
                    var joinKindString = joinKindNodes.ChildNodes.Count > 0
                        ? joinKindNodes.ChildNodes[0].Token.ValueString
                        : "inner";
                    JoinKind joinKind;
                    switch (joinKindString.ToLower())
                    {
                        case "inner":
                            joinKind = JoinKind.Inner;
                            break;
                        case "outer":
                            joinKind = JoinKind.Outer;
                            break;
                        case "left":
                            joinKind = JoinKind.Left;
                            break;
                        case "right":
                            joinKind = JoinKind.Right;
                            break;
                        default:
                            const string messageFormat = "unexpected join kind [{0}]";
                            throw new InvalidOperationException(string.Format(messageFormat, joinKindString));
                    }
                    return new JoinClause
                    {
                        Table = (DeclarationClause) node.ChildNodes[2].AstNode,
                        JoinKind = joinKind,
                        Condition = (ISqlElement) node.ChildNodes[4].AstNode
                    };
                });

            var joinItemList = NonTerminal("joinItemList", null);
            joinItemList.Rule = MakeStarRule(joinItemList, null, joinItem);

            var fromClauseOpt = NonTerminal("fromClauseOpt", termFrom + declaration);

            var whereClauseOpt = NonTerminal("whereClauseOpt",
                Empty | "WHERE" + expression,
                node => node.ChildNodes.Count == 0 ? null : node.ChildNodes[1].AstNode);

            var selectStmt = NonTerminal("selectStmt",
                termSelect + selList + fromClauseOpt + joinItemList + whereClauseOpt,
                delegate(ParseTreeNode n)
                {
                    var elements = n.Elements();
                    var result = new SelectClause
                    {
                        Table = elements.OfType<DeclarationClause>().Single()
                    };
                    var selectColumns = elements.OfType<SelectColumn>().ToArray();
                    if (selectColumns.Length == 0)
                    {
                        result.IsSelectAll = true;
                        result.Columns = null;
                    }
                    else
                        result.Columns.AddRange(selectColumns);
                    result.JoinClauses.AddRange(elements.OfType<JoinClause>());
                    if (n.ChildNodes.Count >= 5)
                        result.WhereExpression = (ISqlElement) n.ChildNodes[4].AstNode;
                    return result;
                });

            RegisterOperators(10, "*", "/", "%");
            RegisterOperators(9, "+", "-");
            RegisterOperators(8, "=", ">", "<", ">=", "<=", "<>", "!=", "!<", "!>", "LIKE", "IN");
            RegisterOperators(7, "^", "&", "|");
            RegisterOperators(5, "AND");
            RegisterOperators(4, "OR");

            MarkPunctuation(",", "(", ")");
            MarkPunctuation(asOpt);

            Root = selectStmt;
        }

        private static NonTerminal NonTerminal(string name, BnfExpression rule)
        {
            return NonTerminal(name, rule, n => new ElementsHolder
            {
                Elements = n.Elements()
            });
        }

        private static NonTerminal NonTerminal(string name, BnfExpression rule, Func<ParseTreeNode, object> creator)
        {
            return new NonTerminal(name, (context, n) => n.AstNode = creator(n))
            {
                Rule = rule
            };
        }
    }
}