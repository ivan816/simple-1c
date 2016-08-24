using NUnit.Framework;
using Simple1C.Impl.Sql;
using Simple1C.Tests.Helpers;

namespace Simple1C.Tests.Sql
{
    public class SqlTranslatorTest : TestBase
    {
        [Test]
        public void Simple()
        {
            const string sourceSql = @"select contractors.ИНН as CounterpartyInn
                from Справочник.Контрагенты as contractors";
            var mappings = @"Справочник.Контрагенты t1
    ИНН c1".Replace("    ", "\t");
            const string expectedResult = @"select contractors.c1 as CounterpartyInn
                from t1 as contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void SimpleWithAlias()
        {
            const string sourceSql = @"select contractors.ИНН as CounterpartyInn
                from Справочник.Контрагенты as contractors";
            var mappings = @"Справочник.Контрагенты t1
    ИНН c1".Replace("    ", "\t");
            const string expectedResult = @"select contractors.c1 as CounterpartyInn
                from t1 as contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void Join()
        {
            const string sourceSql = @"select contracts.ВидДоговора as Kind1, otherContracts.ВидДоговора as Kind2
from справочник.ДоговорыКонтрагентов as contracts
left outer join справочник.ДоговорыКонтрагентов as otherContracts";
            var mappings = @"Справочник.ДоговорыКонтрагентов t1
    ВидДоговора c1".Replace("    ", "\t");
            const string expectedResult = @"select contracts.c1 as Kind1, otherContracts.c1 as Kind2
from t1 as contracts
left outer join t1 as otherContracts";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void Nested()
        {
            const string sourceSql = @"select contracts.владелец.ИНН as ContractorInn
from справочник.ДоговорыКонтрагентов as contracts";

            var mappings = @"Справочник.ДоговорыКонтрагентов t1
    владелец f1 Справочник.Контрагенты
Справочник.Контрагенты t2
    ССылка f2
    ИНН f3".Replace("    ", "\t");

            const string expectedResult = @"select __j_gen_0.f3 as ContractorInn
from t1 as contracts
left join t2 as __j_gen_0 on contracts.f1 = __j_gen_0.f2";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        private static void CheckTranslate(string mappings, string sql, string expectedTranslated)
        {
            var mappingSchema = MappingSchema.Parse(mappings);
            var sqlTranslator = new SqlTranslator();
            var actualTranslated = sqlTranslator.Translate(mappingSchema, sql);
            Assert.That(actualTranslated, Is.EqualTo(expectedTranslated));
        }
    }
}