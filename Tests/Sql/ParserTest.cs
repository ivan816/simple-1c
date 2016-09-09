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

        private static SelectClause Parse(string source)
        {
            var parser = new QueryParser();
            return parser.Parse(source);
        }
    }
}