using System;
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
            Assert.That(aReference.Declaration.Name, Is.EqualTo("testTable"));
            var bReference = (ColumnReferenceExpression) selectClause.Fields[1].Expression;
            Assert.That(bReference.Name, Is.EqualTo("b"));
            Assert.That(bReference.Declaration, Is.SameAs(aReference.Declaration));
            Assert.That(((TableDeclarationClause)selectClause.Source).Name, Is.EqualTo("testTable"));
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
            Assert.That(colReference.Declaration.Name, Is.EqualTo("testTable"));

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
            Assert.That(function.FunctionName, Is.EqualTo(QueryFunctionName.Presentation));

            var columnReference = (ColumnReferenceExpression)function.Arguments[0];
            Assert.That(columnReference.Name, Is.EqualTo("a"));
            Assert.That(columnReference.Declaration.Name, Is.EqualTo("testTable"));
        }
        
        [Test]
        public void DateTimeQueryFunction()
        {
            var selectClause = ParseSelect("select a from testTable where b < DateTime(2010, 11, 12)");
            var binaryExpression = (BinaryExpression)selectClause.WhereExpression;
            var queryFunction = (QueryFunctionExpression) binaryExpression.Right;
            Assert.That(queryFunction.FunctionName, Is.EqualTo(QueryFunctionName.DateTime));
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
            Assert.That(queryFunction.FunctionName, Is.EqualTo(QueryFunctionName.Year));
            Assert.That(queryFunction.Arguments.Count, Is.EqualTo(1));
            Assert.That(((ColumnReferenceExpression)queryFunction.Arguments[0]).Name, Is.EqualTo("c"));
        }
        
        [Test]
        public void QuarterFunction()
        {
            var selectClause = ParseSelect("select a from testTable where b < quArter(c)");
            var binaryExpression = (BinaryExpression)selectClause.WhereExpression;
            var queryFunction = (QueryFunctionExpression) binaryExpression.Right;
            Assert.That(queryFunction.FunctionName, Is.EqualTo(QueryFunctionName.Quarter));
            Assert.That(queryFunction.Arguments.Count, Is.EqualTo(1));
            Assert.That(((ColumnReferenceExpression)queryFunction.Arguments[0]).Name, Is.EqualTo("c"));
        }
        
        [Test]
        public void ValueFunction()
        {
            var selectClause = ParseSelect("select a from testTable where b = value(Перечисление.ЮридическоеФизическоеЛицо.ФизическоеЛицо)");
            var binaryExpression = (BinaryExpression)selectClause.WhereExpression;
            var queryFunction = (ValueLiteralExpression) binaryExpression.Right;
            Assert.That(queryFunction.ObjectName, Is.EqualTo("Перечисление.ЮридическоеФизическоеЛицо.ФизическоеЛицо"));
        }
        
        [Test]
        public void GroupBy()
        {
            var selectClause = ParseSelect("select count(*) from testTable group by c");
            Assert.NotNull(selectClause.GroupBy);
            Assert.That(selectClause.GroupBy.Columns[0].Name, Is.EqualTo("c"));
            Assert.That(selectClause.GroupBy.Columns[0].Declaration.Name, Is.EqualTo("testTable"));
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
            Assert.That(((BinaryExpression)havingClause).Op, Is.EqualTo(SqlBinaryOperator.GreaterThan));

            Assert.That(((AggregateFunctionExpression)left).Function, Is.EqualTo("Count").IgnoreCase);
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
            Assert.That(binaryExpression.Op, Is.EqualTo(SqlBinaryOperator.GreaterThan));
            
            var left = binaryExpression.Left as ColumnReferenceExpression;
            Assert.NotNull(left);
            Assert.That(left.Name, Is.EqualTo("c"));
            Assert.That(left.Declaration.Name, Is.EqualTo("testTable"));
            
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
            Assert.That(binaryExpression.Op, Is.EqualTo(SqlBinaryOperator.And));
            
            var leftAnd = binaryExpression.Left as BinaryExpression;
            Assert.NotNull(leftAnd);
            var col1Reference = leftAnd.Left as ColumnReferenceExpression;
            Assert.NotNull(col1Reference);
            Assert.That(col1Reference.Name, Is.EqualTo("c"));
            Assert.That(col1Reference.Declaration.Name, Is.EqualTo("testTable"));
            var const1Reference = leftAnd.Right as LiteralExpression;
            Assert.NotNull(const1Reference);
            Assert.That(const1Reference.Value, Is.EqualTo(12));

            var rightAnd = binaryExpression.Right as BinaryExpression;
            Assert.NotNull(rightAnd);
            var col2Reference = rightAnd.Left as ColumnReferenceExpression;
            Assert.NotNull(col2Reference);
            Assert.That(col2Reference.Name, Is.EqualTo("c"));
            Assert.That(col2Reference.Declaration, Is.SameAs(col1Reference.Declaration));
            var const2Reference = rightAnd.Right as LiteralExpression;
            Assert.NotNull(const2Reference);
            Assert.That(const2Reference.Value, Is.EqualTo(25));
        }
        
        [Test]
        public void InOperator()
        {
            var selectClause = ParseSelect("select a,b from testTable where c in (10,20,30)");
            var inExpression = selectClause.WhereExpression as InExpression;
            
            Assert.NotNull(inExpression);
            Assert.That(inExpression.Column.Name, Is.EqualTo("c"));
            Assert.That(inExpression.Column.Declaration.Name, Is.EqualTo("testTable"));

            Assert.That(inExpression.Values.Count, Is.EqualTo(3));
            Assert.That(((LiteralExpression) inExpression.Values[0]).Value, Is.EqualTo(10));
            Assert.That(((LiteralExpression) inExpression.Values[1]).Value, Is.EqualTo(20));
            Assert.That(((LiteralExpression) inExpression.Values[2]).Value, Is.EqualTo(30));
        }

        [Test]
        public void AllowExtraneousBracesInExpressions()
        {
            var selectClause = ParseSelect("select (a) from testTable where ((a > 10) and (a > 11))");

            var binaryExperssion = selectClause.WhereExpression as BinaryExpression;
            Assert.NotNull(binaryExperssion);
            var left = binaryExperssion.Left as BinaryExpression;
            var right = binaryExperssion.Right as BinaryExpression;
            Assert.That(binaryExperssion.Op, Is.EqualTo(SqlBinaryOperator.And));
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
            Assert.That(binaryExpression.Op, Is.EqualTo(SqlBinaryOperator.Like));

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
            Assert.That(binaryExpression.Op, Is.EqualTo(SqlBinaryOperator.Neq));

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
            Assert.That(col0Reference.Declaration.Name, Is.EqualTo("testTable1"));
            Assert.That(col0Reference.Declaration.Alias, Is.EqualTo("t1"));

            var col1 = selectClause.Fields[1];
            Assert.That(col1.Alias, Is.EqualTo("nested2"));
            var col1Reference = (ColumnReferenceExpression)col1.Expression;
            Assert.That(col1Reference.Name, Is.EqualTo("b"));
            Assert.That(col1Reference.Declaration.Name, Is.EqualTo("testTable2"));
            Assert.That(col1Reference.Declaration.Alias, Is.EqualTo("t2"));

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
            Assert.That(left.Declaration, Is.SameAs(col0Reference.Declaration));

            var right = binaryExpression.Right as ColumnReferenceExpression;
            Assert.NotNull(right);
            Assert.That(right.Name, Is.EqualTo("id2"));
            Assert.That(right.Declaration, Is.SameAs(col1Reference.Declaration));
        }

        [Test]
        public void ManyJoinClauses()
        {
            var selectClause = ParseSelect(@"select *
from testTable1 as t1
left join testTable2 as t2 on t1.id1 = t2.id2
join testTable3 on t3.id3 = t1.id1
outer join testTable4 as t4 on t4.id4 = t1.id1");

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
            Assert.That(selectClause.JoinClauses[2].JoinKind, Is.EqualTo(JoinKind.Outer));
            Assert.That(joinTable2.Name, Is.EqualTo("testTable4"));
            Assert.That(joinTable2.Alias, Is.EqualTo("t4"));
        }
        
        [Test]
        public void FromAlias()
        {
            var selectClause = ParseSelect("select a,b from testTable as tt");
            var aReference = (ColumnReferenceExpression) selectClause.Fields[0].Expression;
            Assert.That(aReference.Name, Is.EqualTo("a"));
            Assert.That(aReference.Declaration.Name, Is.EqualTo("testTable"));
            Assert.That(aReference.Declaration.Alias, Is.EqualTo("tt"));
            var bReference = (ColumnReferenceExpression) selectClause.Fields[1].Expression;
            Assert.That(bReference.Name, Is.EqualTo("b"));
            Assert.That(bReference.Declaration, Is.SameAs(aReference.Declaration));
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
            Assert.That(columnA.Function, Is.EqualTo("Count").IgnoreCase);
            Assert.That(columnA.IsSelectAll, Is.True);
            Assert.NotNull(columnB);
            Assert.That(columnB.Function, Is.EqualTo("Sum").IgnoreCase);
            Assert.That(columnA.IsSelectAll, Is.True);
        }

        [Test]
        public void AggregateWithColumn()
        {
            var selectClause = ParseSelect("select sum(PaymentSum) from Payments");
            var columnA = selectClause.Fields[0].Expression as AggregateFunctionExpression;
            Assert.NotNull(columnA);
            Assert.That(columnA.Function, Is.EqualTo("Sum").IgnoreCase);
            Assert.That(columnA.Argument, Is.TypeOf<ColumnReferenceExpression>());
            Assert.That(((ColumnReferenceExpression)columnA.Argument).Name, Is.EqualTo("PaymentSum"));
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

        private static SelectClause ParseSelect(string source)
        {
            return Parse(source).GetSingleSelect();
        }

        private static RootClause Parse(string source)
        {
            return new QueryParser().Parse(source);
        }
    }
}