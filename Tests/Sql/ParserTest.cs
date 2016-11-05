using System;
using System.Linq;
using NUnit.Framework;
using Simple1C.Impl.Sql.SqlAccess.Parsing;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Tests.Helpers;

namespace Simple1C.Tests.Sql
{
    public class ParserTest: TestBase
    {
        [Test]
        public void Simple()
        {
            var selectClause = ParseSelect("select a,b from testTable");
            Assert.That(selectClause.IsDistinct, Is.False);
            Assert.That(selectClause.Top, Is.Null);
            Assert.That(selectClause.Fields.Count, Is.EqualTo(2));
            Assert.That(selectClause.Fields[0].Alias, Is.Null);
            var aReference = (ColumnReferenceExpression) selectClause.Fields[0].Expression;
            Assert.That(aReference.Name, Is.EqualTo("a"));
            Assert.That(((TableDeclarationClause)aReference.Table).Name, Is.EqualTo("testTable"));
            var bReference = (ColumnReferenceExpression) selectClause.Fields[1].Expression;
            Assert.That(bReference.Name, Is.EqualTo("b"));
            Assert.That(bReference.Table, Is.SameAs(aReference.Table));
            Assert.That(((TableDeclarationClause)selectClause.Source).Name, Is.EqualTo("testTable"));
        }
        
        [Test]
        public void CanSelectFieldWithAggregateFunctionName()
        {
            var selectWithColumn = ParseSelect("select Сумма from testTable");
            var columnReferenceExpression = selectWithColumn.Fields[0].Expression as ColumnReferenceExpression;
            Assert.IsNotNull(columnReferenceExpression);
            Assert.That(columnReferenceExpression.Name, Is.EqualTo("Сумма"));

            var selectWithAggregate = ParseSelect("select Сумма(a) from testTable");
            var aggregateFunctionExpression = selectWithAggregate.Fields[0].Expression as AggregateFunctionExpression;
            Assert.IsNotNull(aggregateFunctionExpression);
            Assert.That(aggregateFunctionExpression.Function, Is.EqualTo(AggregationFunction.Sum));

            columnReferenceExpression = aggregateFunctionExpression.Argument as ColumnReferenceExpression;
            Assert.IsNotNull(columnReferenceExpression);
            Assert.That(columnReferenceExpression.Name, Is.EqualTo("a"));
        }
        
        [Test]
        public void IgnoreComments()
        {
            var selectClause = ParseSelect("select a/*,b*/ from testTable --where b > 10\r\n //group by a");
            Assert.That(selectClause.WhereExpression, Is.Null);
            Assert.That(selectClause.GroupBy, Is.Null);
            Assert.That(selectClause.Fields.Count, Is.EqualTo(1));
            var columnReference = selectClause.Fields[0].Expression as ColumnReferenceExpression;
            Assert.IsNotNull(columnReference);
            Assert.That(columnReference.Name, Is.EqualTo("a"));
        }

        [Test]
        public void CanParseNull()
        {
            var selectClause = ParseSelect("select null x from testTable");
            var nullLiteral = selectClause.Fields[0].Expression as LiteralExpression;
            Assert.NotNull(nullLiteral);
            Assert.Null(nullLiteral.Value);
        }
        
        [Test]
        public void Bool()
        {
            var selectClause = ParseSelect("select a,b from testTable where c = false");
            var binaryExpression = selectClause.WhereExpression as BinaryExpression;
            Assert.NotNull(binaryExpression);
            var colReference = binaryExpression.Left as ColumnReferenceExpression;
            Assert.NotNull(colReference);
            Assert.That(colReference.Name, Is.EqualTo("c"));
            Assert.That(((TableDeclarationClause)colReference.Table).Name, Is.EqualTo("testTable"));

            var literalExpression = binaryExpression.Right as LiteralExpression;
            Assert.NotNull(literalExpression);
            Assert.That(literalExpression.Value, Is.False);
        }
        
        [Test]
        public void Parenthesis()
        {
            var selectClause = ParseSelect("select (a.b) from testTable");
            Assert.That(selectClause.Fields.Count, Is.EqualTo(1));
            Assert.That(selectClause.Fields[0].Alias, Is.Null);
            var aReference = (ColumnReferenceExpression) selectClause.Fields[0].Expression;
            Assert.That(aReference.Name, Is.EqualTo("a.b"));
        }

        [Test]
        public void PresentationQueryFunction()
        {
            var selectClause = ParseSelect("select Presentation(a) x from testTable");
            Assert.That(selectClause.Fields.Count, Is.EqualTo(1));
            Assert.That(selectClause.Fields[0].Alias, Is.EqualTo("x"));
            var function = (QueryFunctionExpression) selectClause.Fields[0].Expression;
            Assert.That(function.KnownFunction, Is.EqualTo(KnownQueryFunction.Presentation));

            var columnReference = (ColumnReferenceExpression)function.Arguments[0];
            Assert.That(columnReference.Name, Is.EqualTo("a"));
            Assert.That(((TableDeclarationClause)columnReference.Table).Name, Is.EqualTo("testTable"));
        }
        
        [Test]
        public void DateTimeQueryFunction()
        {
            var selectClause = ParseSelect("select a from testTable where b < DateTime(2010, 11, 12)");
            var binaryExpression = (BinaryExpression)selectClause.WhereExpression;
            var queryFunction = (QueryFunctionExpression) binaryExpression.Right;
            Assert.That(queryFunction.KnownFunction, Is.EqualTo(KnownQueryFunction.DateTime));
            Assert.That(queryFunction.Arguments.Count, Is.EqualTo(3));
            Assert.That(((LiteralExpression)queryFunction.Arguments[0]).Value, Is.EqualTo(2010));
            Assert.That(((LiteralExpression)queryFunction.Arguments[1]).Value, Is.EqualTo(11));
            Assert.That(((LiteralExpression)queryFunction.Arguments[2]).Value, Is.EqualTo(12));
        }
        
        [Test]
        public void YearFunction()
        {
            var selectClause = ParseSelect("select a from testTable where b < year(c)");
            var binaryExpression = (BinaryExpression)selectClause.WhereExpression;
            var queryFunction = (QueryFunctionExpression) binaryExpression.Right;
            Assert.That(queryFunction.KnownFunction, Is.EqualTo(KnownQueryFunction.Year));
            Assert.That(queryFunction.Arguments.Count, Is.EqualTo(1));
            Assert.That(((ColumnReferenceExpression)queryFunction.Arguments[0]).Name, Is.EqualTo("c"));
        }
        
        [Test]
        public void QuarterFunction()
        {
            var selectClause = ParseSelect("select a from testTable where b < quArter(c)");
            var binaryExpression = (BinaryExpression)selectClause.WhereExpression;
            var queryFunction = (QueryFunctionExpression) binaryExpression.Right;
            Assert.That(queryFunction.KnownFunction, Is.EqualTo(KnownQueryFunction.Quarter));
            Assert.That(queryFunction.Arguments.Count, Is.EqualTo(1));
            Assert.That(((ColumnReferenceExpression)queryFunction.Arguments[0]).Name, Is.EqualTo("c"));
        }
        
        [Test]
        public void ValueFunction()
        {
            var selectClause = ParseSelect("select a from testTable where b = value(Перечисление.ЮридическоеФизическоеЛицо.ФизическоеЛицо)");
            var binaryExpression = (BinaryExpression)selectClause.WhereExpression;
            var queryFunction = (ValueLiteralExpression) binaryExpression.Right;
            Assert.That(queryFunction.Value, Is.EqualTo("Перечисление.ЮридическоеФизическоеЛицо.ФизическоеЛицо"));
        }
        
        [Test]
        public void GroupByColumn()
        {
            var selectClause = ParseSelect("select count(*) from testTable group by c");
            Assert.NotNull(selectClause.GroupBy);
            var columnReference = (ColumnReferenceExpression) selectClause.GroupBy.Expressions[0];
            Assert.NotNull(columnReference);
            Assert.That(columnReference.Name, Is.EqualTo("c"));
            Assert.That(((TableDeclarationClause)columnReference.Table).Name, Is.EqualTo("testTable"));
        }
        
        [Test]
        public void DistinctInAggregateFunction()
        {
            var selectClause = ParseSelect("select count(distinct a) from testTable group by b");
            var aggregateFunctionExpression = selectClause.Fields[0].Expression as AggregateFunctionExpression;
            Assert.NotNull(aggregateFunctionExpression);
            Assert.That(aggregateFunctionExpression.IsDistinct);
            var aggregateFunctionColumn = aggregateFunctionExpression.Argument as ColumnReferenceExpression;
            Assert.NotNull(aggregateFunctionColumn);
            Assert.That(aggregateFunctionColumn.Name, Is.EqualTo("a"));
            Assert.That(((TableDeclarationClause)aggregateFunctionColumn.Table).Name, Is.EqualTo("testTable"));
        }
        
        [Test]
        public void Negation()
        {
            var selectClause = ParseSelect("select -sum(a) * 1 from testTable group by b");
            var e1 = selectClause.Fields[0].Expression as BinaryExpression;
            Assert.NotNull(e1);
            Assert.That(e1.Operator, Is.EqualTo(SqlBinaryOperator.Mult));

            var e2 = e1.Left as UnaryExpression;
            Assert.NotNull(e2);
            Assert.That(e2.Operator, Is.EqualTo(UnaryOperator.Negation));
        }
        
        [Test]
        public void CanGroupByExpression()
        {
            var selectClause = ParseSelect("select count(*) from testTable group by (c+1), presentation(d)");
            Assert.NotNull(selectClause.GroupBy);
            Assert.That(selectClause.GroupBy.Expressions[0], Is.TypeOf<BinaryExpression>());
            Assert.That(selectClause.GroupBy.Expressions[1], Is.TypeOf<QueryFunctionExpression>());
        }

        [Test]
        public void OrderBy()
        {
            var selectClause = Parse("select * from testTable order by FirstName, LastName asc, Patronymic desc");
            Assert.NotNull(selectClause.OrderBy);
            var orderings = selectClause.OrderBy.Expressions;
            Assert.That(((ColumnReferenceExpression)orderings[0].Expression).Name, Is.EqualTo("FirstName"));
            Assert.That(orderings[1].IsAsc, Is.True);
            Assert.That(((ColumnReferenceExpression)orderings[1].Expression).Name, Is.EqualTo("LastName"));
            Assert.That(orderings[1].IsAsc, Is.True);
            Assert.That(((ColumnReferenceExpression)orderings[2].Expression).Name, Is.EqualTo("Patronymic"));
            Assert.That(orderings[2].IsAsc, Is.False);
        }

        [Test]
        public void Having()
        {
            var selectClause = ParseSelect("select * from testTable group by FirstName having count(Id) > 1");

            var havingClause = selectClause.Having;
            Assert.That(havingClause, Is.TypeOf<BinaryExpression>());

            var left = ((BinaryExpression)havingClause).Left;
            Assert.That(left, Is.TypeOf<AggregateFunctionExpression>());
            Assert.That(((BinaryExpression)havingClause).Operator, Is.EqualTo(SqlBinaryOperator.GreaterThan));

            Assert.That(((AggregateFunctionExpression)left).Function, Is.EqualTo(AggregationFunction.Count));
            Assert.That(((AggregateFunctionExpression)left).Argument, Is.TypeOf<ColumnReferenceExpression>());

            var right = ((BinaryExpression)havingClause).Right;
            Assert.That(right, Is.TypeOf<LiteralExpression>());
            Assert.That(((LiteralExpression)right).Value, Is.EqualTo(1));
        }

        [Test]
        public void Union()
        {
            var rootClause = Parse(@"select a1 from t1
union
select a1 from t2
union all
select a1 from t3
order by a1");
            Assert.That(rootClause, Is.Not.Null);
	        Assert.That(rootClause.Unions.Count, Is.EqualTo(3));
            var union = rootClause.Unions[0];
            Assert.That(union.Type, Is.EqualTo(UnionType.Distinct));
            Assert.That(union.SelectClause.Source, Is.TypeOf<TableDeclarationClause>());
            Assert.That(((TableDeclarationClause) union.SelectClause.Source).Name, Is.EqualTo("t1"));

            union = rootClause.Unions[1];
            Assert.That(union.Type, Is.EqualTo(UnionType.All));
            Assert.That(union.SelectClause.Source, Is.TypeOf<TableDeclarationClause>());
            Assert.That(((TableDeclarationClause)union.SelectClause.Source).Name, Is.EqualTo("t2"));

            union = rootClause.Unions[2];
            Assert.That(union.Type, Is.Null);
            Assert.That(union.SelectClause.Source, Is.TypeOf<TableDeclarationClause>());
            Assert.That(((TableDeclarationClause)union.SelectClause.Source).Name, Is.EqualTo("t3"));

            Assert.That(rootClause.OrderBy, Is.Not.Null);
            Assert.That(((ColumnReferenceExpression) rootClause.OrderBy.Expressions[0].Expression).Name,
                Is.EqualTo("a1"));
        }

        [Test]
        public void WhereCondition()
        {
            var selectClause = ParseSelect("select a,b from testTable where c > 12");
            var binaryExpression = selectClause.WhereExpression as BinaryExpression;
            
            Assert.NotNull(binaryExpression);
            Assert.That(binaryExpression.Operator, Is.EqualTo(SqlBinaryOperator.GreaterThan));
            
            var left = binaryExpression.Left as ColumnReferenceExpression;
            Assert.NotNull(left);
            Assert.That(left.Name, Is.EqualTo("c"));
            Assert.That(((TableDeclarationClause)left.Table).Name, Is.EqualTo("testTable"));
            
            var right = binaryExpression.Right as LiteralExpression;
            Assert.NotNull(right);
            Assert.That(right.Value, Is.EqualTo(12));
        }
        
        [Test]
        public void AndOperator()
        {
            var selectClause = ParseSelect("select * from testTable where c > 12 aNd c<25");
            var binaryExpression = selectClause.WhereExpression as BinaryExpression;
            
            Assert.NotNull(binaryExpression);
            Assert.That(binaryExpression.Operator, Is.EqualTo(SqlBinaryOperator.And));
            
            var leftAnd = binaryExpression.Left as BinaryExpression;
            Assert.NotNull(leftAnd);
            var col1Reference = leftAnd.Left as ColumnReferenceExpression;
            Assert.NotNull(col1Reference);
            Assert.That(col1Reference.Name, Is.EqualTo("c"));
            Assert.That(((TableDeclarationClause)col1Reference.Table).Name, Is.EqualTo("testTable"));
            var const1Reference = leftAnd.Right as LiteralExpression;
            Assert.NotNull(const1Reference);
            Assert.That(const1Reference.Value, Is.EqualTo(12));

            var rightAnd = binaryExpression.Right as BinaryExpression;
            Assert.NotNull(rightAnd);
            var col2Reference = rightAnd.Left as ColumnReferenceExpression;
            Assert.NotNull(col2Reference);
            Assert.That(col2Reference.Name, Is.EqualTo("c"));
            Assert.That(col2Reference.Table, Is.SameAs(col1Reference.Table));
            var const2Reference = rightAnd.Right as LiteralExpression;
            Assert.NotNull(const2Reference);
            Assert.That(const2Reference.Value, Is.EqualTo(25));
        }

        [Test]
        public void UnaryNotOperator()
        {
            var andQuery = ParseSelect("select * from testTable where not b > 0 and c > 0");
            var eqUery = ParseSelect("select * from testTable where not b > 0 = c > 0");
            var whereCondition = andQuery.WhereExpression as BinaryExpression;
            Assert.NotNull(whereCondition);
            var unaryExpression = whereCondition.Left as UnaryExpression;
            Assert.NotNull(unaryExpression);
            Assert.That(unaryExpression.Operator, Is.EqualTo(UnaryOperator.Not));
            Assert.That(eqUery.WhereExpression, Is.TypeOf<UnaryExpression>());
        }

        [Test]
        public void InOperator()
        {
            var selectClause = ParseSelect("select a,b from testTable where c in (10, 20, 30)");
            var inExpression = selectClause.WhereExpression as InExpression;

            Assert.NotNull(inExpression);
            Assert.That(inExpression.Column.Name, Is.EqualTo("c"));
            Assert.That(((TableDeclarationClause) inExpression.Column.Table).Name, Is.EqualTo("testTable"));

            var list = inExpression.Source as ListExpression;
            Assert.NotNull(list);
            var elements = list.Elements
                .OfType<LiteralExpression>()
                .Select(c => c.Value)
                .ToArray();
            Assert.That(elements, Is.EqualTo(new[] {10, 20, 30}));
        }

        [Test]
        public void AllowExtraneousBracesInExpressions()
        {
            var selectClause = ParseSelect("select (a) from testTable where ((a > 10) and (a > 11))");

            var binaryExperssion = selectClause.WhereExpression as BinaryExpression;
            Assert.NotNull(binaryExperssion);
            var left = binaryExperssion.Left as BinaryExpression;
            var right = binaryExperssion.Right as BinaryExpression;
            Assert.That(binaryExperssion.Operator, Is.EqualTo(SqlBinaryOperator.And));
            Assert.NotNull(left);
            Assert.NotNull(right);
        }

        [Test]
        public void ExtraneousBracesAroundTableName_ThrowException()
        {
            Assert.Throws<InvalidOperationException>(() => ParseSelect("select (a) from (testTable)"));
        }
        
        [Test]
        public void LikeOperator()
        {
            var selectClause = ParseSelect("select a,b from testTable where c like \"%test%\"");
            var binaryExpression = selectClause.WhereExpression as BinaryExpression;
            
            Assert.NotNull(binaryExpression);
            Assert.That(binaryExpression.Operator, Is.EqualTo(SqlBinaryOperator.Like));

            var right = binaryExpression.Right as LiteralExpression;
            Assert.NotNull(right);
            Assert.That(right.Value, Is.EqualTo("%test%"));
        }
        
        [Test]
        public void StringLiteralWithEscapedQuote()
        {
            var selectClause = ParseSelect("select a,b from testTable where c != \"ООО \"\"Название в кавычках\"\"\"");
            var binaryExpression = selectClause.WhereExpression as BinaryExpression;
            
            Assert.NotNull(binaryExpression);
            Assert.That(binaryExpression.Operator, Is.EqualTo(SqlBinaryOperator.Neq));

            var right = binaryExpression.Right as LiteralExpression;
            Assert.NotNull(right);
            Assert.That(right.Value, Is.EqualTo("ООО \"Название в кавычках\""));
        }

        [Test]
        public void Join()
        {
            var selectClause = ParseSelect(@"select t1.a as nested1, t2.b as nested2
from testTable1 as t1
left join testTable2 as t2 on t1.id1 = t2.id2");
            Assert.That(selectClause.Fields.Count, Is.EqualTo(2));

            var col0 = selectClause.Fields[0];
            Assert.That(col0.Alias, Is.EqualTo("nested1"));
            var col0Reference = (ColumnReferenceExpression)col0.Expression;
            Assert.That(col0Reference.Name, Is.EqualTo("a"));
            Assert.That(((TableDeclarationClause)col0Reference.Table).Name, Is.EqualTo("testTable1"));
            Assert.That(((TableDeclarationClause)col0Reference.Table).Alias, Is.EqualTo("t1"));

            var col1 = selectClause.Fields[1];
            Assert.That(col1.Alias, Is.EqualTo("nested2"));
            var col1Reference = (ColumnReferenceExpression)col1.Expression;
            Assert.That(col1Reference.Name, Is.EqualTo("b"));
            Assert.That(((TableDeclarationClause)col1Reference.Table).Name, Is.EqualTo("testTable2"));
            Assert.That(((TableDeclarationClause)col1Reference.Table).Alias, Is.EqualTo("t2"));

            var mainTable = (TableDeclarationClause) selectClause.Source;
            Assert.That(mainTable.Name, Is.EqualTo("testTable1"));
            Assert.That(mainTable.Alias, Is.EqualTo("t1"));

            var joinTable = (TableDeclarationClause)selectClause.JoinClauses[0].Source;
            Assert.That(selectClause.JoinClauses.Count, Is.EqualTo(1));
            Assert.That(selectClause.JoinClauses[0].JoinKind, Is.EqualTo(JoinKind.Left));
            Assert.That(joinTable.Name, Is.EqualTo("testTable2"));
            Assert.That(joinTable.Alias, Is.EqualTo("t2"));

            var binaryExpression = selectClause.JoinClauses[0].Condition as BinaryExpression;
            Assert.NotNull(binaryExpression);
            var left = binaryExpression.Left as ColumnReferenceExpression;
            Assert.NotNull(left);
            Assert.That(left.Name, Is.EqualTo("id1"));
            Assert.That(left.Table, Is.SameAs(col0Reference.Table));

            var right = binaryExpression.Right as ColumnReferenceExpression;
            Assert.NotNull(right);
            Assert.That(right.Name, Is.EqualTo("id2"));
            Assert.That(right.Table, Is.SameAs(col1Reference.Table));
        }

        [Test]
        public void ManyJoinClauses()
        {
            var selectClause = ParseSelect(@"select *
from testTable1 as t1
left join testTable2 as t2 on t1.id1 = t2.id2
join testTable3 on t3.id3 = t1.id1
full outer join testTable4 as t4 on t4.id4 = t1.id1");

            Assert.That(selectClause.JoinClauses.Count, Is.EqualTo(3));
            var joinTable0 = (TableDeclarationClause)selectClause.JoinClauses[0].Source;
            Assert.That(selectClause.JoinClauses[0].JoinKind, Is.EqualTo(JoinKind.Left));
            Assert.That(joinTable0.Name, Is.EqualTo("testTable2"));
            Assert.That(joinTable0.Alias, Is.EqualTo("t2"));

            var joinTable1 = (TableDeclarationClause)selectClause.JoinClauses[1].Source;
            Assert.That(selectClause.JoinClauses[1].JoinKind, Is.EqualTo(JoinKind.Inner));
            Assert.That(joinTable1.Name, Is.EqualTo("testTable3"));
            Assert.That(joinTable1.Alias, Is.Null);

            var joinTable2 = (TableDeclarationClause)selectClause.JoinClauses[2].Source;
            Assert.That(selectClause.JoinClauses[2].JoinKind, Is.EqualTo(JoinKind.Full));
            Assert.That(joinTable2.Name, Is.EqualTo("testTable4"));
            Assert.That(joinTable2.Alias, Is.EqualTo("t4"));
        }
        
        [Test]
        public void FromAlias()
        {
            var selectClause = ParseSelect("select a,b from testTable as tt");
            var aReference = (ColumnReferenceExpression) selectClause.Fields[0].Expression;
            Assert.That(aReference.Name, Is.EqualTo("a"));
            Assert.That(((TableDeclarationClause)aReference.Table).Name, Is.EqualTo("testTable"));
            Assert.That(((TableDeclarationClause)aReference.Table).Alias, Is.EqualTo("tt"));
            var bReference = (ColumnReferenceExpression) selectClause.Fields[1].Expression;
            Assert.That(bReference.Name, Is.EqualTo("b"));
            Assert.That(bReference.Table, Is.SameAs(aReference.Table));
            Assert.That(((TableDeclarationClause)selectClause.Source).Name, Is.EqualTo("testTable"));
        }

        [Test]
        public void ColumnAliases()
        {
            var selectClause = ParseSelect("select a as a_alias,b b_alias from testTable");
            Assert.That(selectClause.Fields[0].Alias, Is.EqualTo("a_alias"));
            Assert.That(selectClause.Fields[1].Alias, Is.EqualTo("b_alias"));
        }

        [Test]
        public void SelectAll()
        {
            var selectClause = ParseSelect("select * from testTable");
            Assert.That(selectClause.IsSelectAll, Is.True);
            Assert.That(selectClause.Fields, Is.Null);
        }

        [Test]
        public void AggregateWithWildcard()
        {
            var selectClause = ParseSelect("select count(*) as a, Sum(*) AS b from testTable");
            var columnA = selectClause.Fields[0].Expression as AggregateFunctionExpression;
            var columnB = selectClause.Fields[1].Expression as AggregateFunctionExpression;
            Assert.NotNull(columnA);
            Assert.That(columnA.Function, Is.EqualTo(AggregationFunction.Count));
            Assert.That(columnA.IsSelectAll, Is.True);
            Assert.NotNull(columnB);
            Assert.That(columnB.Function, Is.EqualTo(AggregationFunction.Sum));
            Assert.That(columnA.IsSelectAll, Is.True);
        }

        [Test]
        public void AggregateWithColumnExpression()
        {
            var selectClause = ParseSelect("select sum(PaymentSum*2) from Payments");
            var aggregateArg = selectClause.Fields[0].Expression as AggregateFunctionExpression;
            Assert.NotNull(aggregateArg);
            Assert.That(aggregateArg.Function, Is.EqualTo(AggregationFunction.Sum));
            var binary = aggregateArg.Argument as BinaryExpression;
            Assert.NotNull(binary);
            var left = binary.Left as ColumnReferenceExpression;
            var right = binary.Right as LiteralExpression;
            Assert.NotNull(left);
            Assert.NotNull(right);
            Assert.That(binary.Operator, Is.EqualTo(SqlBinaryOperator.Mult));
        }

        [Test]
        public void Top()
        {
            Assert.That(ParseSelect("select top 1 * from Payments").Top, Is.EqualTo(1));
            Assert.That(ParseSelect("select * from Payments").Top, Is.Null);
        }

        [Test]
        public void SelectDistinct()
        {
            var selectClause = ParseSelect("select distinct * from Payments");
            Assert.That(selectClause.IsDistinct, Is.True);
        }

        [Test]
        public void FilterByNullCondition()
        {
            var selectWhereNull = ParseSelect("select * from Payments where Contractor is null");
            var selectNotNull = ParseSelect("select * from Payments where Contractor is not null");
            
            var isNullExpression = selectWhereNull.WhereExpression as IsNullExpression;
            Assert.NotNull(isNullExpression);
            Assert.That(isNullExpression.Argument, Is.TypeOf<ColumnReferenceExpression>());
            Assert.That(isNullExpression.IsNotNull, Is.False);

            var notNullExpression = selectNotNull.WhereExpression as IsNullExpression;
            Assert.NotNull(notNullExpression);
            Assert.That(notNullExpression.Argument, Is.TypeOf<ColumnReferenceExpression>());
            Assert.That(notNullExpression.IsNotNull, Is.True);
        }

        [Test]
        public void SelectFromSubquery()
        {
            var selectStatement = ParseSelect("select DocumentSum from (select DocumentSum from Payments) t " +
                                           "where t.DocumentSum > 0");

            var subqueryTable = selectStatement.Source as SubqueryTable;
            Assert.NotNull(subqueryTable);
            
            var selectedField = selectStatement.Fields[0].Expression as ColumnReferenceExpression;
            Assert.NotNull(selectedField);
            Assert.That(subqueryTable.Alias, Is.EqualTo("t"));
            Assert.That(selectedField.Name, Is.EqualTo("DocumentSum"));
            Assert.That(selectedField.Table, Is.EqualTo(subqueryTable));

            var whereExpression = selectStatement.WhereExpression as BinaryExpression;
            Assert.NotNull(whereExpression);
            var left = whereExpression.Left as ColumnReferenceExpression;
            Assert.NotNull(left);
            Assert.That(left.Table, Is.EqualTo(subqueryTable));
        }

        [Test]
        public void SubqueryWithoutAlias_ThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() => ParseSelect("select * from (select * from Contractors)"));
        }

        [Test]
        public void EmbeddedQueryInFilterExpression()
        {
            var query = ParseSelect("select number from documents where counterparty in " +
                                           "(select id from counterparty where inn is not null)");

            var inExpression = query.WhereExpression as InExpression;
            Assert.NotNull(inExpression);
            Assert.That(inExpression.Column.Name, Is.EqualTo("counterparty"));
            Assert.That(inExpression.Column.Table, Is.EqualTo(query.Source));

            var innerQuery = ((SubqueryClause) inExpression.Source).Query.GetSingleSelect();
            Assert.That(innerQuery.Source, Is.TypeOf<TableDeclarationClause>());
            Assert.That(((TableDeclarationClause)innerQuery.Source).Name, Is.EqualTo("counterparty"));
            var subqueryColumn = innerQuery.Fields.First().Expression as ColumnReferenceExpression;
            Assert.NotNull(subqueryColumn);
            Assert.That(((TableDeclarationClause)subqueryColumn.Table).Name, Is.EqualTo("counterparty"));
        }

        [Test]
        public void EmbeddedQueryCanReferToOuterTables()
        {
            var query = ParseSelect(@"
SELECT *
FROM table1 t1
WHERE table2Key =
      (SELECT id
       FROM table2 t2
       WHERE id = t1.table2Key
             AND table3Key =
                 (SELECT id
                  FROM table3 t3
                  WHERE id = t2.table3Key AND table1Key = t1.table3Key
                 ))");

            var table1 = query.Source;
            var table1Filter = (BinaryExpression) query.WhereExpression;
            AssertIsColumnReference(table1Filter.Left, "table2Key", table1);
            Assert.That(table1Filter.Right, Is.TypeOf<SubqueryClause>());

            var table2Query = ((SubqueryClause)table1Filter.Right).Query.GetSingleSelect();
            var table2 = (TableDeclarationClause) table2Query.Source;
            Assert.That(table2.Name, Is.EqualTo("table2"));

            var table2Filter1 = (BinaryExpression)((BinaryExpression) table2Query.WhereExpression).Left;
            AssertIsColumnReference(table2Filter1.Left, "id", table2);
            AssertIsColumnReference(table2Filter1.Right, "table2Key", table1);

            var table2Filter2 = (BinaryExpression)((BinaryExpression)table2Query.WhereExpression).Right;
            AssertIsColumnReference(table2Filter2.Left, "table3Key", table2);
            Assert.That(table2Filter2.Right, Is.TypeOf<SubqueryClause>());

            var table3Query = ((SubqueryClause)table2Filter2.Right).Query.GetSingleSelect();
            var table3 = (TableDeclarationClause) table3Query.Source;
            Assert.That(table3.Name, Is.EqualTo("table3"));
            var table3Filter1 = (BinaryExpression)((BinaryExpression)table3Query.WhereExpression).Left;
            AssertIsColumnReference(table3Filter1.Left, "id", table3);
            AssertIsColumnReference(table3Filter1.Right, "table3Key", table2);

            var table3Filter2 = (BinaryExpression)((BinaryExpression)table3Query.WhereExpression).Right;
            AssertIsColumnReference(table3Filter2.Left, "table1Key", table3);
            AssertIsColumnReference(table3Filter2.Right, "table3Key", table1);
        }

        private static SelectClause ParseSelect(string source)
        {
            return Parse(source).GetSingleSelect();
        }

        private static SqlQuery Parse(string source)
        {
            return new QueryParser().Parse(source);
        }

        private static void AssertIsColumnReference(ISqlElement element, string name, ISqlElement source)
        {
            Assert.That(element, Is.TypeOf<ColumnReferenceExpression>());
            var columnReference = (ColumnReferenceExpression) element;
            Assert.That(columnReference.Table, Is.EqualTo(source));
            Assert.That(columnReference.Name, Is.EqualTo(name));
        }
    }
}