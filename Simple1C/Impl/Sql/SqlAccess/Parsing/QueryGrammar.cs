using System;
using System.Collections.Generic;
using System.Linq;
using Irony.Parsing;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    public class QueryGrammar : Grammar
    {
        private const string englishAlphabet = "abcdefghijklmnopqrstuvwxyz";
        private const string russianAlphabet = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";

        private static readonly string validChars = englishAlphabet +
                                                    englishAlphabet.ToUpper() +
                                                    russianAlphabet +
                                                    russianAlphabet.ToUpper() +
                                                    "_";

        private readonly NumberLiteral numberLiteral =
            new NumberLiteral("number", NumberOptions.Default,
                (context, node) => node.AstNode = new LiteralExpression {Value = node.Token.Value});

        public QueryGrammar()
            : base(false)
        {
            LanguageFlags = LanguageFlags.CreateAst;

            var identifier = Identifier();

            var root = NonTerminal("root", null, ToSqlQuery);
            var selectStatement = NonTerminal("selectStmt", null, ToSelectClause);
            var expression = Expression(identifier, root);

            var asOpt = NonTerminal("asOpt", Empty | "AS");
            var aliasOpt = NonTerminal("aliasOpt", null);

            var columnItem = NonTerminal("columnItem", null, ToSelectFieldExpression);

            var tableDeclaration = NonTerminal("declaration", null, ToTableDeclaration);

            var joinItemList = Join(tableDeclaration, expression);
            var fromClause = NonTerminal("fromClause", null, TermFlags.IsTransient);
            var whereClauseOpt = NonTerminal("whereClauseOpt", null,
                node => node.ChildNodes.Count == 0 ? null : node.ChildNodes[1].AstNode);

            var groupClauseOpt = GroupBy(expression);
            var orderClauseOpt = OrderBy(expression);
            var havingClauseOpt = NonTerminal("havingClauseOpt", Empty | ToTerm("having") + expression,
                node => node.ChildNodes.Count == 0 ? null : node.ChildNodes[1].AstNode);
            var columnItemList = NonTerminal("columnItemList", null);

            var topOpt = NonTerminal("topOpt", null);
            var distinctOpt = NonTerminal("distinctOpt", null);
            var selectList = NonTerminal("selectList", null);
            var unionStmtOpt = NonTerminal("unionStmt", null, ToUnionType);
            var unionList = NonTerminal("unionList", null, ToUnionList);

            var subquery = NonTerminal("subQuery", null, ToSubquery);
            //rules
            selectStatement.Rule = ToTerm("SELECT")
                                   + topOpt
                                   + distinctOpt
                                   + selectList
                                   + ToTerm("FROM") + fromClause
                                   + joinItemList + whereClauseOpt
                                   + groupClauseOpt + havingClauseOpt;
            selectList.Rule = columnItemList | "*";
            topOpt.Rule = Empty | ("top" + numberLiteral);
            distinctOpt.Rule = Empty | "distinct";

            aliasOpt.Rule = Empty | asOpt + identifier;
            columnItem.Rule = expression + aliasOpt;
            tableDeclaration.Rule = identifier + aliasOpt;
            columnItemList.Rule = MakePlusRule(columnItemList, ToTerm(","), columnItem);
            subquery.Rule = ToTerm("(") + root + ")" + aliasOpt;
            fromClause.Rule = tableDeclaration | subquery;
            whereClauseOpt.Rule = Empty | ("WHERE" + expression);
            unionStmtOpt.Rule = Empty | ("UNION" + NonTerminal("unionAllModifier", Empty | "ALL"));
            unionList.Rule = MakePlusRule(unionList, null, NonTerminal("unionListElement", selectStatement + unionStmtOpt));
            root.Rule = unionList + orderClauseOpt;

            RegisterOperators(10, "*", "/", "%");
            RegisterOperators(9, "+", "-");
            RegisterOperators(8, "=", ">", "<", ">=", "<=", "<>", "!=", "LIKE", "IN");
            RegisterOperators(5, "AND");
            RegisterOperators(4, "OR");
            MarkPunctuation(",", "(", ")");
            MarkPunctuation(asOpt);
           
            Root = root;
        }

        private static SelectFieldExpression ToSelectFieldExpression(ParseTreeNode n)
        {
            var aliasNodes = n.ChildNodes[1].ChildNodes;
            return new SelectFieldExpression
            {
                Expression = (ISqlElement) n.ChildNodes[0].AstNode,
                Alias = aliasNodes.Count > 0 ? ((Identifier) aliasNodes[0].AstNode).Value : null
            };
        }

        private static TableDeclarationClause ToTableDeclaration(ParseTreeNode n)
        {
            return new TableDeclarationClause
            {
                Name = ((Identifier) n.ChildNodes[0].AstNode).Value,
                Alias = n.ChildNodes[1].Elements().OfType<Identifier>().Select(x => x.Value).SingleOrDefault()
            };
        }

        private NonTerminal Identifier()
        {
            var idSimple = new IdentifierTerminal("Identifier")
            {
                AllFirstChars = validChars,
                AllChars = validChars + "1234567890"
            };
            idSimple.SetFlag(TermFlags.NoAstNode);

            var id = NonTerminal("identifier", null, n => new Identifier
            {
                Value = n.ChildNodes.Select(x => x.Token.ValueString).JoinStrings(".")
            });
            id.Rule = MakePlusRule(id, ToTerm("."), idSimple);
            return id;
        }

        private NonTerminal Expression(NonTerminal identifier, NonTerminal selectStatement)
        {
            var stringLiteral = new StringLiteral("string",
                "\"",
                StringOptions.AllowsAllEscapes | StringOptions.AllowsDoubledQuote,
                (context, node) => node.AstNode = new LiteralExpression {Value = node.Token.Value});

            var valueLiteral = NonTerminal("valueLiteral",
                ToTerm("value") + "(" + identifier + ")",
                node => new ValueLiteralExpression
                {
                    ObjectName = ((Identifier) node.ChildNodes[1].AstNode).Value
                });

            var boolLiteral = NonTerminal("boolLiteral",
                ToTerm("true") | ToTerm("false"),
                node => new LiteralExpression
                {
                    Value = node.FindTokenAndGetText() == "true"
                });
            var columnRef = NonTerminal("columnRef",
                identifier,
                n => new ColumnReferenceExpression
                {
                    Name = ((Identifier)n.ChildNodes[0].AstNode).Value
                });

            var term = NonTerminal("term", null, TermFlags.IsTransient);
            var binOp = NonTerminal("binOp", null, ToBinaryOperator);
            binOp.SetFlag(TermFlags.InheritPrecedence);

            var inExpr = NonTerminal("inExpr", null, ToInExpression);

            var binExpr = NonTerminal("binExpr", null, ToBinaryExpression);
            var exprList = NonTerminal("exprList", null);
            var aggregateFunctionName = NonTerminal("aggregationFunctionName", null, TermFlags.IsTransient);
            var aggregateArg = NonTerminal("aggregateArg", null, TermFlags.IsTransient);
            var aggregate = NonTerminal("aggregate", null, ToAggregateFunction);
            var queryFunctionExpr = NonTerminal("queryFunctionExpr", null, ToQueryFunctionExpression);
            var queryFunctionName = NonTerminal("queryFunctionName", null, ToQueryFunctionName);
            
            var isNullExpression = NonTerminal("isNullExpression", null, ToIsNullExpression);
            var isNull = NonTerminal("isNull", null, TermFlags.NoAstNode);
            var expression = NonTerminal("expression", null, TermFlags.IsTransient);

            var unExpr = NonTerminal("unExpr", null, ToUnaryExpression);
            var parSelectStmt = NonTerminal("parSelectStatement", null,
                node => new SubqueryClause {Query = (SqlQuery) node.ChildNodes[0].AstNode});
            var unOp = NonTerminal("unOp", null);
            var functionArgs = NonTerminal("funArgs", null, TermFlags.IsTransient);
            var parExpr = NonTerminal("parExpr", null, TermFlags.IsTransient);
            var parExprList = NonTerminal("parExprList", null, TermFlags.IsTransient);
            //rules
            exprList.Rule = MakePlusRule(exprList, ToTerm(","), expression);
            parExprList.Rule = "(" + exprList + ")";
            parExpr.Rule = "(" + expression + ")";
            term.Rule = columnRef | stringLiteral | numberLiteral | valueLiteral | boolLiteral
                        | aggregate | queryFunctionExpr
                        | parExpr | parSelectStmt;
            parSelectStmt.Rule = "(" + selectStatement + ")";
            unExpr.Rule = unOp + term;
            unOp.Rule = "NOT"; //| "+" | "-" | "~";
            binOp.Rule = ToTerm("+") | "-" | "=" | ">" | "<" | ">=" | "<=" | "<>" | "!=" | "AND" | "OR" | "LIKE";
            binExpr.Rule = expression + binOp + expression;
            expression.Rule = term | binExpr | inExpr | isNullExpression;

            functionArgs.Rule = parExprList | parSelectStmt;
            
            queryFunctionName.Rule = ToTerm("presentation") | "DATETIME" | "YEAR" | "QUARTER" | "NOT";
            queryFunctionExpr.Rule = queryFunctionName + functionArgs;
            
            aggregateFunctionName.Rule = ToTerm("Count") | "Min" | "Max" | "Sum" | "Avg";
            aggregateArg.Rule = ToTerm("(")+ "*" + ")" | functionArgs;
            aggregate.Rule = aggregateFunctionName + aggregateArg;
            inExpr.Rule = columnRef + ToTerm("in") + functionArgs;

            isNull.Rule = ToTerm("IS") + (Empty | "NOT") + "NULL";
            isNullExpression.Rule = term + isNull;
            
            return expression;
        }

        private static SqlQuery ToSqlQuery(ParseTreeNode node)
        {
            return new SqlQuery
            {
                Unions = (List<UnionClause>)node.ChildNodes[0].AstNode,
                OrderBy = (OrderByClause)(node.ChildNodes.Count > 1 ? node.ChildNodes[1].AstNode : null)
            };
        }

        private NonTerminal GroupBy(NonTerminal expression)
        {
            var groupColumnList = NonTerminal("groupColumnList", null);
            var groupClauseOpt = NonTerminal("groupClauseOpt",
                null,
                node => node.ChildNodes.Count == 0
                    ? null
                    : new GroupByClause
                    {
                        Expressions = node.ChildNodes[2].Elements()
                            .Cast<ISqlElement>()
                            .ToList()
                    });
            groupColumnList.Rule = MakePlusRule(groupColumnList, ToTerm(","), expression);
            groupClauseOpt.Rule = Empty | "GROUP" + ToTerm("BY") + groupColumnList;
            return groupClauseOpt;
        }

        private NonTerminal Join(BnfTerm declaration, BnfTerm expression)
        {
            var joinKindOpt = NonTerminal("joinKindOpt", Empty | "OUTER" | "INNER" | "LEFT" | "RIGHT");
            var joinItem = NonTerminal("joinItem",
                joinKindOpt + "JOIN" + declaration + "ON" + expression,
                ToJoinClause);

            var joinItemList = NonTerminal("joinItemList", null);
            joinItemList.Rule = MakeStarRule(joinItemList, null, joinItem);
            return joinItemList;
        }

        private static InExpression ToInExpression(ParseTreeNode node)
        {
            var sourceNode = node.ChildNodes[2];
            var sqlSource = sourceNode.AstNode as ISqlElement;
            return new InExpression
            {
                Column = (ColumnReferenceExpression) node.ChildNodes[0].AstNode,
                Source = sqlSource ?? new ListExpression
                {
                    Elements = sourceNode.Elements().Cast<ISqlElement>().ToList()
                }
            };
        }

        private static QueryFunctionExpression ToQueryFunctionExpression(ParseTreeNode node)
        {
            return new QueryFunctionExpression
            {
                FunctionName = (QueryFunctionName) node.ChildNodes[0].AstNode,
                Arguments = node.ChildNodes[1].Elements().Cast<ISqlElement>().ToList()
            };
        }

        private static List<UnionClause> ToUnionList(ParseTreeNode node)
        {
            return node.ChildNodes.Select(child =>
            {
                var elements = child.Elements();
                return new UnionClause
                {
                    SelectClause = (SelectClause) elements[0],
                    Type = (UnionType?) (elements.Count > 1 ? elements[1] : null)
                };
            }).ToList();
        }

        private NonTerminal OrderBy(BnfTerm expression)
        {
            var orderingExpression = NonTerminal("orderByColumn",
                expression + NonTerminal("orderingDirection", Empty | "asc" | "desc"),
                ToOrderingElement);
            var orderColumnList = NonTerminal("orderColumnList", null);
            orderColumnList.Rule = MakePlusRule(orderColumnList, ToTerm(","), orderingExpression);
            return NonTerminal("orderClauseOpt",
                Empty | "ORDER" + ToTerm("BY") + orderColumnList,
                ToOrderByClause);
        }

        private static OrderByClause ToOrderByClause(ParseTreeNode node)
        {
            return node.ChildNodes.Count == 0
                ? null
                : new OrderByClause
                {
                    Expressions = node.ChildNodes[2].Elements()
                        .Cast<OrderByClause.OrderingElement>()
                        .ToList()
                };
        }

        private static OrderByClause.OrderingElement ToOrderingElement(ParseTreeNode node)
        {
            var orderExpression = node.ChildNodes[0].AstNode as ISqlElement;
            var isDesc = node.ChildNodes.Count > 1 &&
                         node.ChildNodes[1].FindTokenAndGetText().EqualsIgnoringCase("desc");
            return new OrderByClause.OrderingElement
            {
                Expression = orderExpression,
                IsAsc = !isDesc
            };
        }

        private static SelectClause ToSelectClause(ParseTreeNode n)
        {
            var elements = n.Elements();
            var result = new SelectClause
            {
                Source = (ISqlElement) n.ChildNodes[5].AstNode
            };

            var selectColumns = elements.OfType<SelectFieldExpression>().ToArray();
            if (selectColumns.Length == 0)
            {
                result.IsSelectAll = true;
                result.Fields = null;
            }
            else
                result.Fields.AddRange(selectColumns);
            var topNode = n.ChildNodes[1].ChildNodes.ElementAtOrDefault(1);
            result.Top = topNode != null && topNode.Token != null ? int.Parse(topNode.Token.ValueString) : (int?) null;
            result.IsDistinct = n.ChildNodes[2].ChildNodes.Any();
            result.JoinClauses.AddRange(elements.OfType<JoinClause>());
            result.WhereExpression = (ISqlElement) n.ChildNodes[7].AstNode;
            result.GroupBy = (GroupByClause)n.ChildNodes[8].AstNode;
            result.Having = (ISqlElement)n.ChildNodes[9].AstNode;
            return result;
        }

        private static UnionType? ToUnionType(ParseTreeNode node)
        {
            if (node.ChildNodes.Count == 0)
                return null;
            var allModifierNodes = node.ChildNodes[1].ChildNodes;
            var unionType = allModifierNodes.Count > 0 &&
                            allModifierNodes[0].Token.ValueString.ToLower() == "all"
                ? UnionType.All
                : UnionType.Distinct;
            return unionType;
        }

        private static JoinClause ToJoinClause(ParseTreeNode node)
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
                Source = (TableDeclarationClause) node.ChildNodes[2].AstNode,
                JoinKind = joinKind,
                Condition = (ISqlElement) node.ChildNodes[4].AstNode
            };
        }

        private static AggregateFunctionExpression ToAggregateFunction(ParseTreeNode node)
        {
            var functionName = node.ChildNodes[0].FindTokenAndGetText();
            var argumentNode = node.ChildNodes[1];
            var argumentText = argumentNode.FindTokenAndGetText();
            var isSelectAll = argumentText.Equals("*");
            if (!isSelectAll && argumentNode.AstNode == null)
                throw new InvalidOperationException(string.Format("Invalid aggregation argument {0}", argumentNode));
            return new AggregateFunctionExpression
            {
                Function = functionName,
                Argument = isSelectAll ? null : (ISqlElement) argumentNode.ChildNodes[0].AstNode,
                IsSelectAll = isSelectAll
            };
        }

        private static SubqueryClause ToSubquery(ParseTreeNode node)
        {
            var alias = node.ChildNodes[1].FindTokenAndGetText();
            return new SubqueryClause
            {
                Query = (SqlQuery) node.ChildNodes[0].AstNode,
                Alias = alias
            };
        }

        private static BinaryExpression ToBinaryExpression(ParseTreeNode node)
        {
            var left = (ISqlElement)node.ChildNodes[0].AstNode;
            var binaryOperator = (SqlBinaryOperator)node.ChildNodes[1].AstNode;
            var right = (ISqlElement)node.ChildNodes[2].AstNode;
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
        }

        private static IsNullExpression ToIsNullExpression(ParseTreeNode arg)
        {
            var notNode = arg.ChildNodes[1].ChildNodes[1];
            var notToken = notNode.ChildNodes.Any() ? notNode.ChildNodes[0].Token : null;
            return new IsNullExpression
            {
                Argument = (ISqlElement) arg.ChildNodes[0].AstNode,
                IsNotNull = notToken != null && notToken.ValueString.EqualsIgnoringCase("not")
            };
        }

        private static SqlBinaryOperator ToBinaryOperator(ParseTreeNode node)
        {
            var operatorText1 = node.FindTokenAndGetText();
            switch (operatorText1.ToLower())
            {
                case "and":
                    return SqlBinaryOperator.And;
                case "or":
                    return SqlBinaryOperator.Or;
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
                case "like":
                    return SqlBinaryOperator.Like;
                case "<>":
                case "!=":
                    return SqlBinaryOperator.Neq;
                default:
                    const string messageFormat1 = "unexpected operator [{0}]";
                    throw new InvalidOperationException(string.Format(messageFormat1, operatorText1));
            }
        }

        private static QueryFunctionName ToQueryFunctionName(ParseTreeNode node)
        {
            var queryFunctionNameString = node.ChildNodes[0].Token.ValueString;
            switch (queryFunctionNameString.ToLower())
            {
                case "presentation":
                    return QueryFunctionName.Presentation;
                case "datetime":
                    return QueryFunctionName.DateTime;
                case "year":
                    return QueryFunctionName.Year;
                case "quarter":
                    return QueryFunctionName.Quarter;
                case "not":
                    return QueryFunctionName.SqlNot;
                default:
                    const string messageFormat = "unexpected function [{0}]";
                    throw new InvalidOperationException(string.Format(messageFormat, queryFunctionNameString));
            }
        }

        private static object ToUnaryExpression(ParseTreeNode node)
        {
            return null;
        }

        private static NonTerminal NonTerminal(string name, BnfExpression rule, TermFlags flags = TermFlags.None)
        {
            var nonTerminal = NonTerminal(name, rule, n => new ElementsHolder
            {
                DebugName = name,
                Elements = n.Elements()
            });
            nonTerminal.SetFlag(flags);
            return nonTerminal;
        }

        private static NonTerminal NonTerminal<T>(string name, BnfExpression rule, Func<ParseTreeNode, T> creator)
        {
            return new NonTerminal(name, (context, n) =>
            {
                try
                {
                    n.AstNode = creator(n);
                }
                catch (Exception e)
                {
                    var input = GetTokens(n).JoinStrings(" ");
                    var message = string.Format("Exception creating ast node from node {0} [{1}]", n, input);
                    throw new Exception(message, e);
                }
            })
            {
                Rule = rule
            };
        }

        private static IEnumerable<Token> GetTokens(ParseTreeNode node)
        {
            if (node.Token != null)
                return new[] {node.Token};
            return node.ChildNodes.SelectMany(GetTokens);
        }
    }
}