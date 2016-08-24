using NUnit.Framework;
using Simple1C.Tests.Helpers;

namespace Simple1C.Tests.Sql
{
    public class SqlFormatterTest : TestBase
    {
        [Test]
        public void Simple()
        {
            Check(
                @"select a as a_alias from test_table_name",
                @"select
    a as a_alias
from
    test_table_name");
        }

        private static void Check(string input, string expectedOutput)
        {
            var node = SqlParseHelpers.Parse(input);
            var actualOutput = SqlFormatter.Format(node);
            Assert.That(actualOutput, Is.EqualTo(expectedOutput.Replace("    ", "\t")));
        }
    }
}