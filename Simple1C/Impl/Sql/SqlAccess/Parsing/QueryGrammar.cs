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
            var columnRef = NonTerminal("columnRef",
                identifier,
                n => new ColumnReferenceExpression
                {
                    Name = ((Identifier) n.ChildNodes[0].AstNode).Value
                });

            var expression = Expression(columnRef, identifier);

            var columnSource = NonTerminal("columnSource", expression);
            columnSource.SetFlag(TermFlags.IsTransient);

            var asOpt = NonTerminal("asOpt", Empty | "AS");
            var aliasOpt = NonTerminal("aliasOpt", null);

            var columnItem = NonTerminal("columnItem", null, n =>
            {
                var astNode = n.ChildNodes[0].AstNode;
                var aliasNodes = n.ChildNodes[1].ChildNodes;
                return new SelectFieldElement
                {
                    Expression = (ISqlElement) astNode,
                    Alias = aliasNodes.Count > 0 ? ((Identifier) aliasNodes[0].AstNode).Value : null
                };
            });

            var declaration = NonTerminal("declaration", null,
                delegate(ParseTreeNode n)
                {
                    return new TableDeclarationClause
                    {
                        Name = ((Identifier) n.ChildNodes[0].AstNode).Value,
                        Alias = n.ChildNodes[1].Elements().OfType<Identifier>().Select(x => x.Value).SingleOrDefault()
                    };
                });

            var joinItemList = Join(declaration, expression);
            var fromClause = NonTerminal("fromClauseOpt", null);
            var whereClauseOpt = NonTerminal("whereClauseOpt", null,
                node => node.ChildNodes.Count == 0 ? null : node.ChildNodes[1].AstNode);

            var groupClauseOpt = GroupBy(columnRef);
            var orderClauseOpt = OrderBy(expression);
            var havingClauseOpt = Having(expression);
            var columnItemList = NonTerminal("columnItemList", null);

            var topOpt = NonTerminal("topOpt", null);
            var distinctOpt = NonTerminal("distinctOpt", null);
            var selectList = NonTerminal("selectList", null);
            var selectStatement = NonTerminal("selectStmt", null, ToSelectClause);

            //rules
            selectStatement.Rule = ToTerm("SELECT")
                                   + topOpt
                                   + distinctOpt
                                   + selectList
                                   + fromClause
                                   + joinItemList + whereClauseOpt
                                   + groupClauseOpt + havingClauseOpt;
            selectList.Rule = columnItemList | "*";
            topOpt.Rule = Empty | ("top" + numberLiteral);
            distinctOpt.Rule = Empty | "distinct";

            aliasOpt.Rule = Empty | asOpt + identifier;
            columnItem.Rule = columnSource + aliasOpt;
            declaration.Rule = identifier + aliasOpt;
            columnItemList.Rule = MakePlusRule(columnItemList, ToTerm(","), columnItem);
            fromClause.Rule = ToTerm("FROM") + declaration;
            whereClauseOpt.Rule = Empty | "WHERE" + expression;

            RegisterOperators(10, "*", "/", "%");
            RegisterOperators(9, "+", "-");
            RegisterOperators(8, "=", ">", "<", ">=", "<=", "<>", "!=", "LIKE", "IN");
            RegisterOperators(5, "AND");
            RegisterOperators(4, "OR");
            MarkPunctuation(",", "(", ")");
            MarkPunctuation(asOpt);
            Root = RootElement(selectStatement, orderClauseOpt);
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

        private NonTerminal Expression(NonTerminal columnRef, NonTerminal identifier)
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
                    Value = node.ChildNodes[0].Token.ValueString.ToLower() == "true"
                });

            var term = NonTerminal("term", null);
            term.SetFlag(TermFlags.IsTransient);
            var binOp = NonTerminal("binOp", null, ToBinaryOperator);
            binOp.SetFlag(TermFlags.InheritPrecedence);

            var inExpr = NonTerminal("inExpr", null,
                node => new InExpression
                {
                    Column = (ColumnReferenceExpression) node.ChildNodes[0].AstNode,
                    Values = node.ChildNodes[2].Elements().Cast<ISqlElement>().ToList()
                });

            var binExpr = NonTerminal("binExpr", null, ToBinaryExpression);
            var exprList = NonTerminal("exprList", null);
            var queryFunctionName = NonTerminal("queryFunctionName", null, ToQueryFunctionName);
            var queryFunctionExpr = NonTerminal("queryFunctionExpr", null,
                node => new QueryFunctionExpression
                {
                    FunctionName = (QueryFunctionName) node.ChildNodes[0].AstNode,
                    Arguments = node.ChildNodes[1].Elements().Cast<ISqlElement>().ToList()
                });

            var aggregateArg = NonTerminal("aggregateArg", null);
            var aggregateName = NonTerminal("aggregateName", null);
            var aggregate = NonTerminal("aggregate", null, ToAggregateFunction);
            var isNullExpression = NonTerminal("isNullExpression", null, ToIsNullExpression);
            var isNull = NonTerminal("isNull", null, TermFlags.NoAstNode);
          
            var expression = NonTerminal("expression", null, n => n.ChildNodes[0].AstNode);
            expression.SetFlag(TermFlags.IsTransient);
            var parExpr = NonTerminal("parExpr", null);
            parExpr.SetFlag(TermFlags.IsTransient);
            
            //rules
            queryFunctionName.Rule = ToTerm("presentation") | "DATETIME" | "YEAR" | "QUARTER" | "NOT";
            queryFunctionExpr.Rule = queryFunctionName + "(" + exprList + ")";
            
            aggregateName.Rule = ToTerm("Count") | "Min" | "Max" | "Sum" | "Avg";
            aggregateArg.Rule = ToTerm("*") | term;
            aggregate.Rule = aggregateName + "(" + aggregateArg + ")";

            term.Rule = boolLiteral | valueLiteral | columnRef | numberLiteral | stringLiteral | parExpr;
            expression.Rule = term | binExpr | inExpr | queryFunctionExpr | aggregate | isNullExpression;
            isNull.Rule = ToTerm("IS") + (Empty | "NOT") + "NULL";
            isNullExpression.Rule = term + isNull;
            parExpr.Rule = ToTerm("(") + expression + ToTerm(")");
            binOp.Rule = ToTerm("+") | "-" | "=" | ">" | "<" | ">=" | "<=" | "<>" | "!=" | "AND" | "OR" | "LIKE";
            binExpr.Rule = expression + binOp + expression;
            exprList.Rule = MakePlusRule(exprList, ToTerm(","), expression);
            inExpr.Rule = columnRef + ToTerm("in") + "(" + exprList + ")";
            return expression;
        }

        private NonTerminal GroupBy(NonTerminal columnRef)
        {
            var groupColumnList = NonTerminal("groupColumnList", null);
            var groupClauseOpt = NonTerminal("groupClauseOpt",
                null,
                node => node.ChildNodes.Count == 0
                    ? null
                    : new GroupByClause
                    {
                        Columns = node.ChildNodes[2].Elements()
                            .Cast<ColumnReferenceExpression>()
                            .ToList()
                    });
            groupColumnList.Rule = MakePlusRule(groupColumnList, ToTerm(","), columnRef);
            groupClauseOpt.Rule = Empty | "GROUP" + ToTerm("BY") + groupColumnList;
            return groupClauseOpt;
        }

        private NonTerminal RootElement(NonTerminal selectStatement, NonTerminal orderClauseOpt)
        {
            var unionStmtOpt = NonTerminal<UnionType?>("unionStmt", null, delegate(ParseTreeNode node)
            {
                if (node.ChildNodes.Count == 0)
                    return null;
                var allModifierNodes = node.ChildNodes[1].ChildNodes;
                var unionType = allModifierNodes.Count > 0 &&
                                allModifierNodes[0].Token.ValueString.ToLower() == "all"
                    ? UnionType.All
                    : UnionType.Distinct;
                return unionType;
            });
            var unionList = NonTerminal("unionList", null, delegate(ParseTreeNode node)
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
            });
            var root = NonTerminal("root", null, node => new RootClause
            {
                Unions = (List<UnionClause>)node.ChildNodes[0].AstNode,
                OrderBy = (OrderByClause)(node.ChildNodes.Count > 1 ? node.ChildNodes[1].AstNode : null)
            });

            //rules
            unionStmtOpt.Rule = Empty | ("UNION" + NonTerminal("unionAllModifier", Empty | "ALL"));
            unionList.Rule = MakePlusRule(unionList, null, NonTerminal("unionListElement", selectStatement + unionStmtOpt));
            root.Rule = unionList + orderClauseOpt;
            return root;
        }

        private NonTerminal Join(BnfTerm declaration, BnfTerm expression)
        {
            var joinKindOpt = NonTerminal("joinKindOpt", Empty | "OUTER" | "INNER" | "LEFT" | "RIGHT");
            var joinItem = NonTerminal("joinItem",
                joinKindOpt + "JOIN" + declaration + "ON" + expression,
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
                        Source = (TableDeclarationClause) node.ChildNodes[2].AstNode,
                        JoinKind = joinKind,
                        Condition = (ISqlElement) node.ChildNodes[4].AstNode
                    };
                });

            var joinItemList = NonTerminal("joinItemList", null);
            joinItemList.Rule = MakeStarRule(joinItemList, null, joinItem);
            return joinItemList;
        }

        private NonTerminal Having(BnfTerm expression)
        {
            return NonTerminal("havingClauseOpt", Empty | ToTerm("having") + expression,
                node => node.ChildNodes.Count == 0 ? null : node.ChildNodes[1].AstNode);
        }

        private NonTerminal OrderBy(BnfTerm expression)
        {
            var orderingExpression = NonTerminal("orderByColumn",
                expression + NonTerminal("orderingDirection", Empty | "asc" | "desc"),
                node =>
                {
                    var orderExpression = node.ChildNodes[0].AstNode as ISqlElement;
                    var isDesc = node.ChildNodes.Count > 1 &&
                                 node.ChildNodes[1].ChildNodes.Count > 0 &&
                                 node.ChildNodes[1].ChildNodes[0].Token.ValueString.EqualsIgnoringCase("desc");
                    return new OrderByClause.OrderingElement
                    {
                        Expression = orderExpression,
                        IsAsc = !isDesc
                    };
                });
            var orderColumnList = NonTerminal("orderColumnList", null);
            orderColumnList.Rule = MakePlusRule(orderColumnList, ToTerm(","), orderingExpression);
            return NonTerminal("orderClauseOpt",
                Empty | "ORDER" + ToTerm("BY") + orderColumnList,
                node => node.ChildNodes.Count == 0
                    ? null
                    : new OrderByClause
                    {
                        Expressions = node.ChildNodes[2].Elements()
                            .Cast<OrderByClause.OrderingElement>()
                            .ToList()
                    });
        }

        private static SelectClause ToSelectClause(ParseTreeNode n)
        {
            var elements = n.Elements();
            var result = new SelectClause
            {
                Source = elements.OfType<TableDeclarationClause>().Single()
            };

            var selectColumns = elements.OfType<SelectFieldElement>().ToArray();
            if (selectColumns.Length == 0)
            {
                result.IsSelectAll = true;
                result.Fields = null;
            }
            else
                result.Fields.AddRange(selectColumns);
            var topSql = n.ChildNodes[1].ChildNodes.Select(c => c.Token.ValueString).JoinStrings(" ");
            result.Top = !string.IsNullOrEmpty(topSql) ? new RawSqlElement {Sql = topSql} : null;
            result.IsDistinct = n.ChildNodes[2].ChildNodes.Any();
            result.JoinClauses.AddRange(elements.OfType<JoinClause>());
            result.WhereExpression = (ISqlElement) n.ChildNodes[6].AstNode;
            result.GroupBy = (GroupByClause)n.ChildNodes[7].AstNode;
            result.Having = (ISqlElement)n.ChildNodes[8].AstNode;
            return result;
        }

        private static AggregateFunction ToAggregateFunction(ParseTreeNode node)
        {
            var functionName = node.ChildNodes[0].ChildNodes[0].Token.ValueString;
            var argumentNode = node.ChildNodes[1].ChildNodes[0];
            var isSelectAll = argumentNode.Token != null && argumentNode.Token.ValueString.Equals("*");
            if (!isSelectAll && argumentNode.AstNode == null)
                throw new InvalidOperationException(string.Format("Invalid aggregation argument {0}", argumentNode));
            return new AggregateFunction
            {
                Function = functionName,
                Argument = (ISqlElement) argumentNode.AstNode,
                IsSelectAll = isSelectAll
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
            var trash = arg.ChildNodes[1].ChildNodes[1];
            var notToken = trash.ChildNodes.Any() ? trash.ChildNodes[0].Token : null;
            return new IsNullExpression
            {
                Argument = (ISqlElement) arg.ChildNodes[0].AstNode,
                IsNotNull = notToken != null && notToken.ValueString.EqualsIgnoringCase("not")
            };
        }

        private static SqlBinaryOperator ToBinaryOperator(ParseTreeNode node)
        {
            var operatorText1 = node.ChildNodes[0].Token.ValueString;
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

        private static NonTerminal NonTerminal(string name, BnfExpression rule, TermFlags flags = TermFlags.None)
        {
            var nonTerminal = NonTerminal(name, rule, n => new ElementsHolder
            {
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
                    var message = string.Format("Exception creating ast node from node {0} at location ({1}:{2})",
                        n, n.Span.Location, n.Span.EndLocation);
                    throw new Exception(message, e);
                }
            })
            {
                Rule = rule
            };
        }
    }
}