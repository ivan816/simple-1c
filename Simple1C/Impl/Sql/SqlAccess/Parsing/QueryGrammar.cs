using System;
using System.Linq;
using Irony.Parsing;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    public class QueryGrammar : Grammar
    {
        private const string englishAlphbet = "abcdefghijklmnopqrstuvwxyz";
        private const string russianAlphbet = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";

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
            var termIn = ToTerm("IN");
            var termBy = ToTerm("BY");
            var termValue = ToTerm("VALUE");
            var termTrue = ToTerm("TRUE");
            var termFalse = ToTerm("FALSE");

            var termRepresentation = ToTerm("PRESENTATION");

            var validChars = englishAlphbet +
                             englishAlphbet.ToUpper() +
                             russianAlphbet +
                             russianAlphbet.ToUpper() +
                             "_";
            var idSimple = new IdentifierTerminal("Identifier")
            {
                AllFirstChars = validChars,
                AllChars = validChars + "1234567890"
            };
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

            var columnRef = NonTerminal("columnRef",
                id,
                n => new ColumnReferenceExpression
                {
                    Name = ((Identifier) n.ChildNodes[0].AstNode).Value
                });

            var stringLiteral = new StringLiteral("string",
                "\"",
                StringOptions.AllowsAllEscapes,
                (context, node) => node.AstNode = new LiteralExpression { Value = node.Token.Value });
            var numberLiteral = new NumberLiteral("number", NumberOptions.Default,
                (context, node) => node.AstNode = new LiteralExpression
                {
                    Value = node.Token.Value
                });

            var valueLiteral = NonTerminal("valueLiteral",
                termValue + "(" + id + ")",
                node => new ValueLiteral
                {
                    ObjectName = ((Identifier) node.ChildNodes[1].AstNode).Value
                });

            var boolLiteral = NonTerminal("boolLiteral",
                termTrue | termFalse,
                node => new LiteralExpression
                {
                    Value = node.ChildNodes[0].Token.ValueString.ToLower() == "true"
                });

            var term = NonTerminal("term", boolLiteral | valueLiteral | columnRef | numberLiteral | stringLiteral);
            term.SetFlag(TermFlags.IsTransient);

            var binOp = NonTerminal("binOp",
                ToTerm("+") | "-" | "=" | ">" | "<" | ">=" | "<=" | "<>" | "!=" | "AND" | "OR" | "LIKE",
                delegate(ParseTreeNode node)
                {
                    var operatorText = node.ChildNodes[0].Token.ValueString;
                    switch (operatorText)
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
                            const string messageFormat = "unexpected operator [{0}]";
                            throw new InvalidOperationException(string.Format(messageFormat, operatorText));
                    }
                });
            binOp.SetFlag(TermFlags.InheritPrecedence);

            var inExpr = NonTerminal("inExpr", null,
                node => new InExpression
                {
                    Column = (ColumnReferenceExpression)node.ChildNodes[0].AstNode,
                    Values = node.ChildNodes[2].Elements().Cast<ISqlElement>().ToList()
                });

            var binExpr = NonTerminal("binExpr", null, delegate(ParseTreeNode node)
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
            });
            var exprList = NonTerminal("exprList", null);
            var queryFunctionName = NonTerminal("queryFunctionName",
                termRepresentation | "DATETIME" | "YEAR" | "QUARTER" | "NOT",
                delegate(ParseTreeNode node)
                {
                    var queryFunctionNameString = node.ChildNodes[0].Token.ValueString.ToLower();
                    switch (queryFunctionNameString)
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
                });
            var queryFunctionExpr = NonTerminal("queryFunctionExpr",
                queryFunctionName + "(" + exprList + ")",
                node => new QueryFunctionExpression
                {
                    FunctionName = (QueryFunctionName) node.ChildNodes[0].AstNode,
                    Arguments = node.ChildNodes[1].Elements().Cast<ISqlElement>().ToList()
                });

            var expression = NonTerminal("expression",
                term | binExpr | inExpr | queryFunctionExpr,
                n => n.ChildNodes[0].AstNode);
            expression.SetFlag(TermFlags.IsTransient);

            binExpr.Rule = expression + binOp + expression;
            exprList.Rule = MakePlusRule(exprList, termComma, expression);
            inExpr.Rule = columnRef + termIn + "(" + exprList + ")";

            var columnSource = NonTerminal("columnSource", expression | aggregate | "(" + expression + ")");
            columnSource.SetFlag(TermFlags.IsTransient);

            var asOpt = NonTerminal("asOpt", Empty | termAs);
            var aliasOpt = NonTerminal("aliasOpt", Empty | asOpt + id);

            var columnItem = NonTerminal("columnItem", columnSource + aliasOpt, n =>
            {
                var astNode = n.ChildNodes[0].AstNode;
                var aliasNodes = n.ChildNodes[1].ChildNodes;
                return new SelectField
                {
                    Expression = (ISqlElement) astNode,
                    Alias = aliasNodes.Count > 0 ? ((Identifier) aliasNodes[0].AstNode).Value : null
                };
            });

            var columnItemList = NonTerminal("columnItemList", null);
            columnItemList.Rule = MakePlusRule(columnItemList, termComma, columnItem);

            var selList = NonTerminal("selList", columnItemList | "*");
            var declaration = NonTerminal("declaration",
                id + aliasOpt,
                delegate(ParseTreeNode n)
                {
                    return new TableDeclarationClause
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
                        Source = (TableDeclarationClause) node.ChildNodes[2].AstNode,
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

            var columnRefList = NonTerminal("columnRefListcolumnRef", null);
            columnRefList.Rule = MakePlusRule(columnRefList, termComma, columnRef);
            var groupClauseOpt = NonTerminal("groupClauseOpt",
                Empty | "GROUP" + termBy + columnRefList,
                node => node.ChildNodes.Count == 0
                    ? null
                    : new GroupByClause
                    {
                        Columns = node.ChildNodes[2].Elements()
                            .Cast<ColumnReferenceExpression>()
                            .ToList()
                    });

            var unionStmt = NonTerminal("unionStmt", null,
                delegate(ParseTreeNode node)
                {
                    if (node.ChildNodes.Count == 0)
                        return null;
                    var allModifierNodes = node.ChildNodes[1].ChildNodes;
                    var unionType = allModifierNodes.Count > 0 &&
                                    allModifierNodes[0].Token.ValueString.ToLower() == "all"
                        ? UnionType.All
                        : UnionType.Distinct;
                    return new UnionClause
                    {
                        Type = unionType,
                        SelectClause = (SelectClause) node.ChildNodes[2].AstNode
                    };
                });

            var selectStmt = NonTerminal("selectStmt",
                termSelect + selList + fromClauseOpt + joinItemList + whereClauseOpt + groupClauseOpt + unionStmt,
                delegate(ParseTreeNode n)
                {
                    var elements = n.Elements();
                    var result = new SelectClause
                    {
                        Source = elements.OfType<TableDeclarationClause>().Single()
                    };
                    var selectColumns = elements.OfType<SelectField>().ToArray();
                    if (selectColumns.Length == 0)
                    {
                        result.IsSelectAll = true;
                        result.Fields = null;
                    }
                    else
                        result.Fields.AddRange(selectColumns);
                    result.JoinClauses.AddRange(elements.OfType<JoinClause>());
                    result.WhereExpression = (ISqlElement) n.ChildNodes[4].AstNode;
                    result.GroupBy = n.ChildNodes[5].AstNode as GroupByClause;
                    result.Union = n.ChildNodes[6].AstNode as UnionClause;
                    return result;
                });
            var unionAllModifier = NonTerminal("unionAllModifier", Empty | "ALL");

            unionStmt.Rule = Empty | ToTerm("UNION") + unionAllModifier + selectStmt;

            RegisterOperators(10, "*", "/", "%");
            RegisterOperators(9, "+", "-");
            RegisterOperators(8, "=", ">", "<", ">=", "<=", "<>", "!=", "LIKE", "IN");
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