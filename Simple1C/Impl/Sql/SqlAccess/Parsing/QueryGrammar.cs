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
            var idSimple = new IdentifierTerminal("Identifier");
            idSimple.SetFlag(TermFlags.NoAstNode);

            //nonterminals
            var selectStmt = NonTerminal("selectStmt", delegate(ParseTreeNode n)
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
                    foreach (var c in selectColumns)
                    {
                        var columnReference = c.Expression as ColumnReferenceExpression;
                        if (columnReference != null)
                            columnReference.TableName = result.Table.Name;
                        result.Columns.Add(c);
                    }
                return result;
            });
            var aliasOpt = NonTerminal("aliasOpt");
            var asOpt = NonTerminal("asOpt");
            var selList = NonTerminal("selList");
            var aggregate = NonTerminal("aggregate", delegate(ParseTreeNode node)
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
            var aggregateName = NonTerminal("aggregateName");
            var aggregateArg = NonTerminal("aggregateArg");
            var columnItemList = NonTerminal("columnItemList");
            var columnSource = NonTerminal("columnSource");
            var columnItem = NonTerminal("columnItem", n =>
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
            var fromClauseOpt = NonTerminal("fromClauseOpt", n => new DeclarationClause
            {
                Name = n.Elements().OfType<Identifier>().Single().Value
            });
            var idlist = NonTerminal("idlist");
            var id = NonTerminal("id", n => new Identifier
            {
                Value = n.ChildNodes.Select(x => x.Token.ValueString).JoinStrings(".")
            });

            //rules
            selectStmt.Rule = termSelect + selList + fromClauseOpt;
            selList.Rule = columnItemList | "*";
            columnItemList.Rule = MakePlusRule(columnItemList, termComma, columnItem);
            columnItem.Rule = columnSource + aliasOpt;
            columnSource.Rule = id | aggregate;
            aggregate.Rule = aggregateName + "(" + aggregateArg + ")";
            aggregateName.Rule = termCount | "Min" | "Max" | "Sum";
            aggregateArg.Rule = "*"; 
            aliasOpt.Rule = Empty | asOpt + id;
            asOpt.Rule = Empty | termAs;
            fromClauseOpt.Rule = termFrom + idlist;
            idlist.Rule = MakePlusRule(idlist, termComma, id);
            id.Rule = MakePlusRule(id, dot, idSimple);

            Root = selectStmt;
        }

        private static NonTerminal NonTerminal(string name)
        {
            return NonTerminal(name, n => new ElementsHolder
            {
                Elements = n.Elements()
            });
        }

        private static NonTerminal NonTerminal(string name, Func<ParseTreeNode, object> creator)
        {
            return new NonTerminal(name, (context, n) => n.AstNode = creator(n));
        }
    }
}