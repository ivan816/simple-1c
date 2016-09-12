using NUnit.Framework;
using Simple1C.Impl.Sql.SqlAccess.Parsing;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Tests.Sql
{
    public class ParserTest
    {
        [Test]
        public void Simple()
        {
            var selectClause = Parse("select a,b from testTable");
            Assert.That(selectClause.Columns.Count, Is.EqualTo(2));
            Assert.That(selectClause.Columns[0].Alias, Is.Null);
            var aReference = (ColumnReferenceExpression) selectClause.Columns[0].Expression;
            Assert.That(aReference.Name, Is.EqualTo("a"));
            Assert.That(aReference.TableName, Is.EqualTo("testTable"));
            var bReference = (ColumnReferenceExpression) selectClause.Columns[1].Expression;
            Assert.That(bReference.Name, Is.EqualTo("b"));
            Assert.That(bReference.TableName, Is.EqualTo("testTable"));
            Assert.That(selectClause.Table.Name, Is.EqualTo("testTable"));
        }
        
        [Test]
        public void Join()
        {
            var selectClause = Parse(@"select t1.a as nested1, t2.b as nested2
from testTable1 as t1
left join testTable2 as t2 on t1.id1 = t2.id2");
            Assert.That(selectClause.Columns.Count, Is.EqualTo(2));

            var col0 = selectClause.Columns[0];
            Assert.That(col0.Alias, Is.EqualTo("nested1"));
            var col0Reference = (ColumnReferenceExpression)col0.Expression;
            Assert.That(col0Reference.Name, Is.EqualTo("a"));
            Assert.That(col0Reference.TableName, Is.EqualTo("t1"));

            var col1 = selectClause.Columns[1];
            Assert.That(col1.Alias, Is.EqualTo("nested2"));
            var col1Reference = (ColumnReferenceExpression)col1.Expression;
            Assert.That(col1Reference.Name, Is.EqualTo("b"));
            Assert.That(col1Reference.TableName, Is.EqualTo("t2"));

            Assert.That(selectClause.Table.Name, Is.EqualTo("testTable1"));
            Assert.That(selectClause.Table.Alias, Is.EqualTo("t1"));

            Assert.That(selectClause.JoinClauses.Count, Is.EqualTo(1));
            Assert.That(selectClause.JoinClauses[0].JoinKind, Is.EqualTo(JoinKind.Left));
            Assert.That(selectClause.JoinClauses[0].Table.Name, Is.EqualTo("testTable2"));
            Assert.That(selectClause.JoinClauses[0].Table.Alias, Is.EqualTo("t2"));

            var binaryExpression = selectClause.JoinClauses[0].Condition as BinaryExpression;
            Assert.NotNull(binaryExpression);
            var left = binaryExpression.Left as ColumnReferenceExpression;
            Assert.NotNull(left);
            Assert.That(left.Name, Is.EqualTo("id1"));
            Assert.That(left.TableName, Is.EqualTo("t1"));

            var right = binaryExpression.Right as ColumnReferenceExpression;
            Assert.NotNull(right);
            Assert.That(right.Name, Is.EqualTo("id2"));
            Assert.That(right.TableName, Is.EqualTo("t2"));
        }
        
        [Test]
        public void FromAlias()
        {
            var selectClause = Parse("select a,b from testTable as tt");
            var aReference = (ColumnReferenceExpression) selectClause.Columns[0].Expression;
            Assert.That(aReference.Name, Is.EqualTo("a"));
            Assert.That(aReference.TableName, Is.EqualTo("tt"));
            var bReference = (ColumnReferenceExpression) selectClause.Columns[1].Expression;
            Assert.That(bReference.Name, Is.EqualTo("b"));
            Assert.That(bReference.TableName, Is.EqualTo("tt"));
            Assert.That(selectClause.Table.Name, Is.EqualTo("testTable"));
        }

        [Test]
        public void ColumnAliases()
        {
            var selectClause = Parse("select a as a_alias,b b_alias from testTable");
            Assert.That(selectClause.Columns[0].Alias, Is.EqualTo("a_alias"));
            Assert.That(selectClause.Columns[1].Alias, Is.EqualTo("b_alias"));
        }

        [Test]
        public void SelectAll()
        {
            var selectClause = Parse("select * from testTable");
            Assert.That(selectClause.IsSelectAll, Is.True);
            Assert.That(selectClause.Columns, Is.Null);
        }

        [Test]
        public void SelectAggregate()
        {
            var selectClause = Parse("select count(*) as a, Sum(*) AS b from testTable");
            var columnA = selectClause.Columns[0].Expression as AggregateFunction;
            var columnB = selectClause.Columns[1].Expression as AggregateFunction;
            Assert.NotNull(columnA);
            Assert.That(columnA.Type, Is.EqualTo(AggregateFunctionType.Count));
            Assert.NotNull(columnB);
            Assert.That(columnB.Type, Is.EqualTo(AggregateFunctionType.Sum));
        }

        private static SelectClause Parse(string source)
        {
            var parser = new QueryParser();
            return parser.Parse(source);
        }
    }
}