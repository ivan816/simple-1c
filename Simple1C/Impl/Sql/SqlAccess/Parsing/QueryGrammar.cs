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

        private readonly BnfExpression not;
        private readonly NonTerminal by;
        private readonly NonTerminal distinctOpt;

        public QueryGrammar()
            : base(false)
        {
            LanguageFlags = LanguageFlags.CreateAst;
            NonGrammarTerminals.Add(new CommentTerminal("sql_style_comment", "/*", "*/"));
            NonGrammarTerminals.Add(new CommentTerminal("sql_style_line_comment", "--", "\n", "\r\n"));
            NonGrammarTerminals.Add(new CommentTerminal("1c_comment", "//", "\n", "\r\n"));

            not = Transient("not", ToTerm("NOT") | "НЕ");
            by = NonTerminal("by", ToTerm("by") | "ПО", TermFlags.NoAstNode);
            distinctOpt = NonTerminal("distinctOpt", null);
            var identifier = Identifier();

            var root = NonTerminal("root", null, ToSqlQuery);
            var selectStatement = NonTerminal("selectStmt", null, ToSelectClause);
            var expression = Expression(identifier, root);

            var asOpt = NonTerminal("asOpt", Empty | "AS" | "КАК");
            var alias = NonTerminal("alias", null, TermFlags.IsTransient);
            var aliasOpt = NonTerminal("aliasOpt", null, TermFlags.IsTransient);

            var columnItem = NonTerminal("columnItem", null, ToSelectFieldExpression);

            var tableDeclaration = NonTerminal("joinSource", null, ToTableDeclaration);

            var columnSource = NonTerminal("columnSource", null, TermFlags.IsTransient);
            var joinItemList = Join(columnSource, expression);
            var whereClauseOpt = NonTerminal("whereClauseOpt", null,
                node => node.ChildNodes.Count == 0 ? null : node.ChildNodes[1].AstNode);

            var groupClauseOpt = GroupBy(expression);
            var orderClauseOpt = OrderBy(expression);
            var havingKeyword = Transient("havingKeyword", ToTerm("HAVING") | "ИМЕЮЩИЕ");
            var havingClauseOpt = NonTerminal("havingClauseOpt", Empty | havingKeyword + expression,
                node => node.ChildNodes.Count == 0 ? null : node.ChildNodes[1].AstNode);
            var columnItemList = NonTerminal("columnItemList", null);

            var topOpt = NonTerminal("topOpt", null);
            var selectList = NonTerminal("selectList", null);
            var unionStmtOpt = NonTerminal("unionStmt", null, ToUnionType);
            var unionList = NonTerminal("unionList", null, ToUnionList);

            var subqueryTable = NonTerminal("subQuery", null, ToSubqueryTable);
            //rules
            selectStatement.Rule = Transient("select", ToTerm("SELECT") | "ВЫБРАТЬ")
                                   + topOpt
                                   + distinctOpt
                                   + selectList
                                   + Transient("from", ToTerm("FROM") | ToTerm("ИЗ")) + columnSource
                                   + joinItemList + whereClauseOpt
                                   + groupClauseOpt + havingClauseOpt;
            selectList.Rule = columnItemList | "*";
            topOpt.Rule = Empty | Transient("top", ToTerm("TOP") | "ПЕРВЫЕ") + numberLiteral;
            distinctOpt.Rule = Empty | Transient("distinct", ToTerm("DISTINCT") | "РАЗЛИЧНЫЕ");

            columnSource.Rule = tableDeclaration | subqueryTable;
            alias.Rule = asOpt + identifier;
            aliasOpt.Rule = Empty | alias;
            columnItem.Rule = expression + aliasOpt;
            tableDeclaration.Rule = identifier + aliasOpt;
            columnItemList.Rule = MakePlusRule(columnItemList, ToTerm(","), columnItem);
            subqueryTable.Rule = ToTerm("(") + root + ")" + alias;
            whereClauseOpt.Rule = Empty | (Transient("where", ToTerm("WHERE") | "ГДЕ") + expression);
            unionStmtOpt.Rule = Empty |
                                (Transient("union", ToTerm("UNION") | "ОБЪЕДИНИТЬ") +
                                 Transient("unionAllModifier", Empty | "ALL" | "ВСЕ"));
            unionList.Rule = MakePlusRule(unionList, null,
                NonTerminal("unionListElement", selectStatement + unionStmtOpt));
            root.Rule = unionList + orderClauseOpt;

            RegisterOperators<SqlBinaryOperator>();
            RegisterOperators<UnaryOperator>();
            MarkPunctuation(",", "(", ")");
            MarkPunctuation(asOpt);
            AddOperatorReportGroup("operator");
            AddToNoReportGroup("as", "КАК");
            Root = root;
        }

        private static SelectFieldExpression ToSelectFieldExpression(ParseTreeNode n)
        {
            return new SelectFieldExpression
            {
                Expression = (ISqlElement) n.ChildNodes[0].AstNode,
                Alias = n.ChildNodes.Count > 1 ? ((Identifier) n.ChildNodes[1].AstNode).Value : null
            };
        }

        private static TableDeclarationClause ToTableDeclaration(ParseTreeNode n)
        {
            return new TableDeclarationClause
            {
                Name = ((Identifier) n.ChildNodes[0].AstNode).Value,
                Alias = n.ChildNodes.Count > 1 ? ((Identifier) n.ChildNodes[1].AstNode).Value : null
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

        private static readonly string[] aggregateFunctions =
        {
            "Count", "Min", "Max", "Sum", "Avg",
            "КОЛИЧЕСТВО", "МИНИМУМ", "МАКСИМУМ", "СУММА", "СРЕДНЕЕ"
        };

        private NonTerminal Expression(NonTerminal identifier, NonTerminal selectStatement)
        {
            var datePartLiteral = NonTerminal("datePartLiteral", null,
                c => new LiteralExpression
                {
                    Value = ToDatePart(c),
                    SqlType = SqlType.DatePart
                });
            var stringLiteral = new StringLiteral("string",
                "\"",
                StringOptions.AllowsAllEscapes | StringOptions.AllowsDoubledQuote,
                (context, node) => node.AstNode = new LiteralExpression {Value = node.Token.Value});

            var valueLiteral = NonTerminal("valueLiteral",
                Transient("valueFunction", ToTerm("value") | "ЗНАЧЕНИЕ") + "(" + identifier + ")",
                node => new ValueLiteralExpression
                {
                    Value = ((Identifier) node.ChildNodes[1].AstNode).Value
                });

            var nullLiteral = NonTerminal("nullLiteral",
                ToTerm("null"), node => new LiteralExpression());

            var boolLiteral = NonTerminal("boolLiteral",
                ToTerm("true") | "false" | "ложь" | "истина",
                node =>
                {
                    var text = node.FindTokenAndGetText().ToLower();
                    return new LiteralExpression
                    {
                        Value = text == "true" || text == "истина"
                    };
                });
            var columnRef = NonTerminal("columnRef",
                identifier,
                n => new ColumnReferenceExpression
                {
                    Name = ((Identifier) n.ChildNodes[0].AstNode).Value
                });

            var term = NonTerminal("term", null, TermFlags.IsTransient);
            var binOp = NonTerminal("binOp", null, ToBinaryOperator);
            binOp.SetFlag(TermFlags.InheritPrecedence);

            var inExpr = NonTerminal("inExpr", null, ToInExpression);

            var binExpr = NonTerminal("binExpr", null, ToBinaryExpression);
            var exprList = NonTerminal("exprList", null);
            var aggregateFunctionName = NonTerminal("aggregationFunctionName", null, ToAggregationFunction);
            var aggregateArg = NonTerminal("aggregateArg", null);
            var aggregate = NonTerminal("aggregate", null, ToAggregateFunctionExpression);
            var queryFunctionExpr = NonTerminal("queryFunctionExpr", null, ToQueryFunctionExpression);

            var isNullExpression = NonTerminal("isNullExpression", null, ToIsNullExpression);
            var isReferenceExpression = NonTerminal("isReference",
                columnRef + "ССЫЛКА" + identifier,
                ToIsReferenceExpression);
            var isNull = NonTerminal("isNull", null, TermFlags.NoAstNode);
            var expression = NonTerminal("expression", null, TermFlags.IsTransient);

            var unExpr = NonTerminal("unExpr", null, ToUnaryExpression);
            var subquery = NonTerminal("parSelectStatement", null, ToSubquery);
            var unOp = NonTerminal("unOp", null, ToUnaryOperator);
            unOp.SetFlag(TermFlags.InheritPrecedence);
            var functionArgs = NonTerminal("funArgs", null, TermFlags.IsTransient);
            var parExpr = NonTerminal("parExpr", null, TermFlags.IsTransient);
            var parExprList = NonTerminal("parExprList", null, TermFlags.IsTransient);

            var dateTruncExpression = NonTerminal("dateTruncExpression", null, ToDateTruncExpression);

            var caseElement = NonTerminal("caseElement", null, ToCaseElement);
            var caseElementList = NonTerminal("caseElementList", null);
            var caseExpression = NonTerminal("caseExpression", null, ToCaseExpression);
            var defaultCaseOpt = NonTerminal("defaultCaseOpt", null,
                c => c.ChildNodes.Count > 1 ? c.ChildNodes[1].AstNode : null);

            //rules
            exprList.Rule = MakeStarRule(exprList, ToTerm(","), expression);
            parExprList.Rule = "(" + exprList + ")";
            parExpr.Rule = "(" + expression + ")";
            term.Rule = columnRef | stringLiteral | numberLiteral | valueLiteral | boolLiteral | nullLiteral
                        | aggregate | queryFunctionExpr
                        | parExpr | subquery;
            subquery.Rule = "(" + selectStatement + ")";
            unOp.Rule = not | "-";
            unExpr.Rule = unOp + expression;
            binOp.Rule = ToTerm("+") | "-" | "*" | "/" | "%" |
                         "=" | ">" | "<" | ">=" | "<=" | "<>" | "!="
                         | "AND" | "OR" | "LIKE" | "И" | "ИЛИ" | "ПОДОБНО";
            binExpr.Rule = expression + binOp + expression;
            expression.Rule = term | unExpr | binExpr
                              | inExpr | isNullExpression | isReferenceExpression
                              | dateTruncExpression | caseExpression;

            functionArgs.Rule = parExprList | subquery;

            queryFunctionExpr.Rule = identifier + functionArgs;

            aggregateFunctionName.Rule = ToTerm(aggregateFunctions[0]);
            for (var i = 1; i < aggregateFunctions.Length; i++)
                aggregateFunctionName.Rule |= aggregateFunctions[i];

            aggregateArg.Rule = ToTerm("*") | distinctOpt + expression;
            aggregate.Rule = aggregateFunctionName + "(" + aggregateArg + ")";
            inExpr.Rule = columnRef + Transient("in", ToTerm("IN") | "В") + functionArgs;

            datePartLiteral.Rule = ToTerm("year") | "quarter" | "month" | "week" | "day" | "hour" | "minute"
                                   | "ГОД" | "КВАРТАЛ" | "МЕСЯЦ" | "НЕДЕЛЯ" | "ДЕНЬ" | "ЧАС" | "МИНУТА";
            dateTruncExpression.Rule = Transient("beginOfPeriod", ToTerm("beginOfPeriod") | "НачалоПериода")
                                       + ToTerm("(") + expression + "," + datePartLiteral + ")";

            caseElement.Rule = Transient("when", ToTerm("WHEN") | "КОГДА") + expression +
                               Transient("then", ToTerm("THEN") | "ТОГДА") + expression;
            caseElementList.Rule = MakeListRule(caseElementList, Empty, caseElement);
            caseExpression.Rule = Transient("case", ToTerm("CASE") | "ВЫБОР") + caseElementList +
                                  defaultCaseOpt + Transient("end", ToTerm("END") | "КОНЕЦ");
            defaultCaseOpt.Rule = Empty | (Transient("else", ToTerm("ELSE") | "ИНАЧЕ") + expression);

            isNull.Rule = NonTerminal("is", ToTerm("IS") | "ЕСТЬ") + (Empty | not) + "NULL";
            isNullExpression.Rule = term + isNull;

            return expression;
        }

        private static SqlQuery ToSqlQuery(ParseTreeNode node)
        {
            return new SqlQuery
            {
                Unions = (List<UnionClause>) node.ChildNodes[0].AstNode,
                OrderBy = (OrderByClause) (node.ChildNodes.Count > 1 ? node.ChildNodes[1].AstNode : null)
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
            groupClauseOpt.Rule = Empty |
                                  NonTerminal("group", ToTerm("GROUP") | "СГРУППИРОВАТЬ") + by + groupColumnList;
            return groupClauseOpt;
        }

        private NonTerminal Join(BnfTerm columnSource, BnfTerm joinCondition)
        {
            var joinKindOpt = NonTerminal("joinKindOpt", null);
            var joinItem = NonTerminal("joinItem", null, ToJoinClause);
            var joinItemList = NonTerminal("joinItemList", null);
            var outerJoinKind = NonTerminal("outerJoinKind", null, TermFlags.IsTransient);
            var outerKeywordOpt = NonTerminal("outerKeywordOpt", null, TermFlags.NoAstNode | TermFlags.IsPunctuation);
            var on = NonTerminal("on", ToTerm("ON") | "ПО", TermFlags.NoAstNode);
            outerKeywordOpt.Rule = ToTerm("OUTER") | "ВНЕШНЕЕ" | Empty;
            outerJoinKind.Rule = ToTerm("FULL") | "LEFT" | "RIGHT" | "ПОЛНОЕ" | "ЛЕВОЕ" | "ПРАВОЕ";
            joinItem.Rule = joinKindOpt + Transient("join", ToTerm("JOIN") | "СОЕДИНЕНИЕ") + columnSource + on +
                            joinCondition;

            joinKindOpt.Rule = Empty | Transient("inner", ToTerm("INNER") | "ВНУТРЕННЕЕ") |
                               (outerJoinKind + outerKeywordOpt);
            joinItemList.Rule = MakeStarRule(joinItemList, null, joinItem);

            return joinItemList;
        }

        private static CaseExpression ToCaseExpression(ParseTreeNode node)
        {
            return new CaseExpression
            {
                Elements = node.ChildNodes[1].Elements().Cast<CaseElement>().ToList(),
                DefaultValue = (ISqlElement) node.ChildNodes[2].AstNode
            };
        }

        private static CaseElement ToCaseElement(ParseTreeNode node)
        {
            return new CaseElement
            {
                Condition = (ISqlElement) node.ChildNodes[1].AstNode,
                Value = (ISqlElement) node.ChildNodes[3].AstNode
            };
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

        private static QueryFunctionExpression ToDateTruncExpression(ParseTreeNode node)
        {
            return new QueryFunctionExpression
            {
                KnownFunction = KnownQueryFunction.SqlDateTrunc,
                Arguments = node.Elements().OfType<ISqlElement>().ToList()
            };
        }

        private static QueryFunctionExpression ToQueryFunctionExpression(ParseTreeNode node)
        {
            var functionName = ((Identifier) node.ChildNodes[0].AstNode).Value;
            var queryFunction = ToQueryFunctionName(functionName);
            return new QueryFunctionExpression
            {
                KnownFunction = queryFunction,
                CustomFunction = queryFunction == null ? functionName : null,
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
                expression + NonTerminal("orderingDirection", Empty | "ASC" | "DESC" | "УБЫВ" | "ВОЗР"),
                ToOrderingElement);
            var orderColumnList = NonTerminal("orderColumnList", null);
            orderColumnList.Rule = MakePlusRule(orderColumnList, ToTerm(","), orderingExpression);
            return NonTerminal("orderClauseOpt",
                Empty | Transient("order", ToTerm("ORDER") | "УПОРЯДОЧИТЬ") + by + orderColumnList,
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
            var isAsc = true;
            if (node.ChildNodes.Count > 1)
            {
                var token = node.ChildNodes[1].FindTokenAndGetText();
                isAsc = !(token.EqualsIgnoringCase("desc") | token.EqualsIgnoringCase("убыв"));
            }

            return new OrderByClause.OrderingElement
            {
                Expression = orderExpression,
                IsAsc = isAsc
            };
        }

        private static SelectClause ToSelectClause(ParseTreeNode n)
        {
            var elements = n.Elements();
            var result = new SelectClause
            {
                Source = (IColumnSource) n.ChildNodes[5].AstNode
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
            result.GroupBy = (GroupByClause) n.ChildNodes[8].AstNode;
            result.Having = (ISqlElement) n.ChildNodes[9].AstNode;
            return result;
        }

        private static UnionType? ToUnionType(ParseTreeNode node)
        {
            if (node.ChildNodes.Count == 0)
                return null;
            if (node.ChildNodes.Count == 1)
                return UnionType.Distinct;
            var allModifier = node.ChildNodes[1].FindTokenAndGetText();
            return !string.IsNullOrEmpty(allModifier)
                   && (allModifier.EqualsIgnoringCase("all") || allModifier.EqualsIgnoringCase("все"))
                ? UnionType.All
                : UnionType.Distinct;
        }

        private static JoinClause ToJoinClause(ParseTreeNode node)
        {
            var joinKindString = node.ChildNodes[0].FindTokenAndGetText() ?? "inner";
            JoinKind joinKind;
            switch (joinKindString.ToLower())
            {
                case "inner":
                case "внутреннее":
                    joinKind = JoinKind.Inner;
                    break;
                case "full":
                case "полное":
                    joinKind = JoinKind.Full;
                    break;
                case "left":
                case "левое":
                    joinKind = JoinKind.Left;
                    break;
                case "right":
                case "правое":
                    joinKind = JoinKind.Right;
                    break;
                default:
                    throw new InvalidOperationException(string.Format("unexpected join kind [{0}]", joinKindString));
            }
            return new JoinClause
            {
                Source = (IColumnSource) node.ChildNodes[2].AstNode,
                JoinKind = joinKind,
                Condition = (ISqlElement) node.ChildNodes[4].AstNode
            };
        }

        private static AggregateFunctionExpression ToAggregateFunctionExpression(ParseTreeNode node)
        {
            var argumentNode = node.ChildNodes[1];
            var isSelectAll = false;
            var isDistinct = false;
            if (argumentNode.ChildNodes.Count == 1)
            {
                var argumentText = argumentNode.ChildNodes[0].Token.Text;
                if (argumentText != "*")
                    throw new InvalidOperationException("assertion failure");
                isSelectAll = true;
            }
            else
            {
                var distinctText = argumentNode.ChildNodes[0].FindTokenAndGetText();
                if (!string.IsNullOrEmpty(distinctText))
                    isDistinct = true;
            }
            if (!isSelectAll && argumentNode.AstNode == null)
                throw new InvalidOperationException(string.Format("Invalid aggregation argument {0}", argumentNode));
            return new AggregateFunctionExpression
            {
                Function = (AggregationFunction) node.ChildNodes[0].AstNode,
                Argument = isSelectAll ? null : (ISqlElement) argumentNode.ChildNodes[1].AstNode,
                IsSelectAll = isSelectAll,
                IsDistinct = isDistinct
            };
        }

        private static AggregationFunction ToAggregationFunction(ParseTreeNode node)
        {
            var text = node.FindTokenAndGetText().ToLower();
            switch (text)
            {
                case "количество":
                case "count":
                    return AggregationFunction.Count;
                case "сумма":
                case "sum":
                    return AggregationFunction.Sum;
                case "минимум":
                case "min":
                    return AggregationFunction.Min;
                case "максимум":
                case "max":
                    return AggregationFunction.Max;
                case "среднее":
                case "avg":
                    return AggregationFunction.Avg;
                default:
                    throw new InvalidOperationException(string.Format("Invalid aggregation function {0}", text));
            }
        }

        private static SubqueryTable ToSubqueryTable(ParseTreeNode node)
        {
            var alias = node.ChildNodes[1].FindTokenAndGetText();
            return new SubqueryTable
            {
                Query = new SubqueryClause {Query = ToSqlQuery(node.ChildNodes[0])},
                Alias = alias
            };
        }

        private static BinaryExpression ToBinaryExpression(ParseTreeNode node)
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
        }

        private static SubqueryClause ToSubquery(ParseTreeNode node)
        {
            return new SubqueryClause
            {
                Query = (SqlQuery) node.ChildNodes[0].AstNode
            };
        }

        private static IsNullExpression ToIsNullExpression(ParseTreeNode arg)
        {
            var notNode = arg.ChildNodes[1].ChildNodes[1];
            var notToken = notNode.ChildNodes.Any() ? notNode.ChildNodes[0].Token : null;
            return new IsNullExpression
            {
                Argument = (ISqlElement) arg.ChildNodes[0].AstNode,
                IsNotNull = notToken != null && (notToken.ValueString.EqualsIgnoringCase("not")
                                                 || notToken.ValueString.EqualsIgnoringCase("не"))
            };
        }

        private static IsReferenceExpression ToIsReferenceExpression(ParseTreeNode arg)
        {
            return new IsReferenceExpression
            {
                Argument = (ColumnReferenceExpression) arg.ChildNodes[0].AstNode,
                ObjectName = ((Identifier) arg.ChildNodes[2].AstNode).Value
            };
        }

        private static SqlBinaryOperator ToBinaryOperator(ParseTreeNode node)
        {
            var operatorText = node.FindTokenAndGetText().ToLower();
            switch (operatorText)
            {
                case "and":
                case "и":
                    return SqlBinaryOperator.And;
                case "or":
                case "или":
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
                case "подобно":
                    return SqlBinaryOperator.Like;
                case "<>":
                case "!=":
                    return SqlBinaryOperator.Neq;
                case "*":
                    return SqlBinaryOperator.Mult;
                case "/":
                    return SqlBinaryOperator.Div;
                case "%":
                    return SqlBinaryOperator.Remainder;
                default:
                    throw new InvalidOperationException(string.Format("unexpected binary operator [{0}]", operatorText));
            }
        }

        private static UnaryOperator ToUnaryOperator(ParseTreeNode node)
        {
            var text = node.FindTokenAndGetText().ToLower();
            switch (text)
            {
                case "not":
                case "не":
                    return UnaryOperator.Not;
                case "-":
                    return UnaryOperator.Negation;
                default:
                    throw new InvalidOperationException(string.Format("unexpected unary operator [{0}]", text));
            }
        }

        private static KnownQueryFunction? ToQueryFunctionName(string name)
        {
            switch (name.ToLower())
            {
                case "presentation":
                case "представление":
                    return KnownQueryFunction.Presentation;
                case "datetime":
                case "датавремя":
                    return KnownQueryFunction.DateTime;
                case "year":
                case "год":
                    return KnownQueryFunction.Year;
                case "quarter":
                case "квартал":
                    return KnownQueryFunction.Quarter;
                case "not":
                case "не":
                    return KnownQueryFunction.SqlNot;
                case "isnull":
                case "естьnull":
                    return KnownQueryFunction.IsNull;
                case "substring":
                case "подстрока":
                    return KnownQueryFunction.Substring;
                case "beginofperiod":
                case "началопериода":
                    return KnownQueryFunction.SqlDateTrunc;
                default:
                    return null;
            }
        }

        private static object ToUnaryExpression(ParseTreeNode node)
        {
            return new UnaryExpression
            {
                Operator = (UnaryOperator) node.ChildNodes[0].AstNode,
                Argument = (ISqlElement) node.ChildNodes[1].AstNode
            };
        }

        private static DatePart ToDatePart(ParseTreeNode node)
        {
            var text = node.FindTokenAndGetText().ToLower();
            switch (text)
            {
                case "год":
                case "year":
                    return DatePart.Year;
                case "квартал":
                case "quarter":
                    return DatePart.Quarter;
                case "месяц":
                case "month":
                    return DatePart.Month;
                case "неделя":
                case "week":
                    return DatePart.Week;
                case "день":
                case "day":
                    return DatePart.Day;
                case "час":
                case "hour":
                    return DatePart.Hour;
                case "минута":
                case "minute":
                    return DatePart.Minute;
                default:
                    throw new InvalidOperationException("Unexpected date part literal " + node);
            }
        }

        private static NonTerminal Transient(string name, BnfExpression rule)
        {
            return new NonTerminal(name, rule) {Flags = TermFlags.IsTransient};
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
                    const string messageFormat = "exception creating ast node from node {0} [{1}]";
                    throw new InvalidOperationException(string.Format(messageFormat, n, input), e);
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

        private void RegisterOperators<TEnum>() where TEnum : struct
        {
            foreach (var value in Enum.GetValues(typeof(TEnum)).Cast<TEnum>())
            {
                var precedence = EnumAttributesCache<OperatorPrecedenceAttribute>.GetAttribute(value).Precedence;
                var opSymbols = EnumAttributesCache<OperatorSynonymsAttribute>.GetAttribute(value).Synonyms;
                RegisterOperators(precedence, opSymbols);
            }
        }

        public override void CreateTokenFilters(LanguageData language, TokenFilterList filters)
        {
            base.CreateTokenFilters(language, filters);
            filters.Add(new AggregateFunctionTokenFilter());
        }

        private class AggregateFunctionTokenFilter : TokenFilter
        {
            private static readonly HashSet<string> aggregateFunctionsSet =
                aggregateFunctions.ToSet(StringComparer.OrdinalIgnoreCase);

            public override IEnumerable<Token> BeginFiltering(ParsingContext context, IEnumerable<Token> tokens)
            {
                foreach (var token in tokens)
                {
                    if (aggregateFunctionsSet.Contains(token.Text) && context.Parser.Context.Source.PreviewChar != '(')
                        token.KeyTerm = null;
                    yield return token;
                }
            }
        }
    }
}