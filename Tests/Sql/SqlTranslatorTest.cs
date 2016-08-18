using NUnit.Framework;
using Simple1C.Impl.Sql;
using Simple1C.Tests.Helpers;

namespace Simple1C.Tests.Sql
{
    public class SqlTranslatorTest : TestBase
    {
        private SqlTranslator sqlTranslator;

        protected override void SetUp()
        {
            base.SetUp();
            sqlTranslator = new SqlTranslator();
        }

        [Test]
        public void Simple()
        {
            const string sourceSql = @"select ИНН as CounterpartyInn
                from Справочник.Контрагенты";
            const string mappings = @"Справочник.Контрагенты a";
            const string expectedResult = @"";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        private static void CheckTranslate(string mappings, string sql, string expectedTranslated)
        {
            var translator = new SqlTranslator(MappingSchema.Parse(mappings));
            var actualTranslated = translator.Translate(sql);
            Assert.That(actualTranslated, Is.EqualTo(expectedTranslated));
        }
    }
}