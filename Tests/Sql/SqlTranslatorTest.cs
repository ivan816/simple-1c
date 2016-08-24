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
            const string mappings = @"Справочник.Контрагенты t1
    ИНН c1";
            const string expectedResult = @"select contractors._c1 as CounterpartyInn
    from _t1 as contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void SimpleWithAlias()
        {
            const string sourceSql = @"select contractors.ИНН as CounterpartyInn
                from Справочник.Контрагенты as contractors";
            const string mappings = @"Справочник.Контрагенты T1
    ИНН C1";
            const string expectedResult = @"select contractors._c1 as CounterpartyInn
                from _t1 as contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void Join()
        {
            const string sourceSql = @"select contracts.ВидДоговора as Kind1, otherContracts.ВидДоговора as Kind2
from справочник.ДоговорыКонтрагентов as contracts
left outer join справочник.ДоговорыКонтрагентов as otherContracts";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1
    ВидДоговора c1";
            const string expectedResult = @"select contracts._c1 as Kind1, otherContracts._c1 as Kind2
from _t1 as contracts
left outer join _t1 as otherContracts";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void ConvertDoubleQuotesToSingleQuotes()
        {
            const string sourceSql = @"select contractors.ИНН as CounterpartyInn
    from Справочник.Контрагенты as contractors
    where contractors.Наименование = ""test-name""";
            const string mappings = @"Справочник.Контрагенты t1
    ИНН c1
    Наименование c2";
            const string expectedResult = @"select contractors._c1 as CounterpartyInn
    from _t1 as contractors
    where contractors._c2 = 'test-name'";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void Nested()
        {
            const string sourceSql = @"select contracts.наименование, contracts.владелец.ИНН as ContractorInn
from справочник.ДоговорыКонтрагентов as contracts";

            const string mappings = @"Справочник.ДоговорыКонтрагентов t1
    владелец f1 Справочник.Контрагенты
    наименование f4
Справочник.Контрагенты t2
    ССылка f2
    ИНН f3";

            const string expectedResult = @"select contracts._f4, contracts.__nested_field0 as ContractorInn
from (select
    __nested_main_table0._f4,
    __nested_table0._f3 as __nested_field0
from _t1 as __nested_main_table0
left join _t2 as __nested_table0 on __nested_main_table0._f1rref = __nested_table0._f2) as contracts";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }
        
        [Test]
        public void ManyNestedProperties()
        {
            const string sourceSql = @"select contracts.владелец.ИНН as ContractorInn,contracts.владелец.Наименование as ContractorName
from справочник.ДоговорыКонтрагентов as contracts";

            const string mappings = @"Справочник.ДоговорыКонтрагентов t1
    владелец f1 Справочник.Контрагенты
Справочник.Контрагенты t2
    ССылка f2
    ИНН f3
    Наименование f4";

            const string expectedResult = @"select contracts.__nested_field0 as ContractorInn,contracts.__nested_field1 as ContractorName
from (select
    __nested_table0._f3 as __nested_field0,
    __nested_table0._f4 as __nested_field1
from _t1 as __nested_main_table0
left join _t2 as __nested_table0 on __nested_main_table0._f1rref = __nested_table0._f2) as contracts";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        private static void CheckTranslate(string mappings, string sql, string expectedTranslated)
        {
            var mappingSchema = MappingSchema.Parse(SpacesToTabs(mappings));
            var sqlTranslator = new SqlTranslator();
            var actualTranslated = sqlTranslator.Translate(mappingSchema, sql);
            Assert.That(SpacesToTabs(actualTranslated), Is.EqualTo(SpacesToTabs(expectedTranslated)));
        }

        private static string SpacesToTabs(string s)
        {
            return s.Replace("    ", "\t");
        }
    }
}