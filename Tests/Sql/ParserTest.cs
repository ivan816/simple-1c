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