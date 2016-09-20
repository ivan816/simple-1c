using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Impl.Sql.Translation;
using Simple1C.Tests.Helpers;

namespace Simple1C.Tests
{
    internal class Class1 : TestBase
    {
        [Test]
        public void Test1()
        {
            var inputs = new[] { "C:\\Users\\mskr\\Desktop\\badQuery.1c"}
                .Select(c=>File.ReadAllText(c, Encoding.UTF8))
                .ToArray();
            var translator =
                new QueryToSqlTranslator(
                    new PostgreeSqlSchemaStore(
                        new PostgreeSqlDatabase("host=localhost;port=5432;Database=mskr;Username=mskr;Password=developer")),
                    new int[0]);
            foreach (var input in inputs)
            {
                Console.WriteLine(translator.Translate(input));
            }
        }
    }
}