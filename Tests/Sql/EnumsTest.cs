using NUnit.Framework;

namespace Simple1C.Tests.Sql
{
    public class EnumsTest : TranslationTestBase
    {
        [Test]
        public void EnumsNoText()
        {
            const string sourceSql =
                @"select contractors.НаименованиеПолное as ContractorFullname,contractors.ЮридическоеФизическоеЛицо as ContractorType
from справочник.Контрагенты as contractors";

            const string mappings = @"Справочник.Контрагенты t1 Main
    наименованиеполное Single f1
    ЮридическоеФизическоеЛицо Single f2 Перечисление.ЮридическоеФизическоеЛицо
Перечисление.ЮридическоеФизическоеЛицо t2 Main
    ССылка Single f3
    Порядок Single f4";

            const string expectedResult = @"select
    contractors.f1 as ContractorFullname,
    contractors.f2 as ContractorType
from t1 as contractors";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void EnumsWithText()
        {
            const string sourceSql =
                @"select contractors.НаименованиеПолное as ContractorFullname,ПРЕДСТАВЛЕНИЕ(contractors.ЮридическоеФизическоеЛицо) as ContractorTypeText,contractors.ЮридическоеФизическоеЛицо as ContractorType
from справочник.Контрагенты as contractors";

            const string mappings = @"Справочник.Контрагенты t1 Main
    наименованиеполное Single f1
    ЮридическоеФизическоеЛицо Single f2 Перечисление.ЮридическоеФизическоеЛицо
Перечисление.ЮридическоеФизическоеЛицо t2 Main
    ССылка Single f3
    Порядок Single f4";

            const string expectedResult =
                @"select
    contractors.f1 as ContractorFullname,
    contractors.__nested_field0 as ContractorTypeText,
    contractors.f2 as ContractorType
from (select
    __nested_table0.f1,
    __nested_table2.enumValueName as __nested_field0,
    __nested_table0.f2
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.f3 = __nested_table0.f2
left join simple1c__enumMappings as __nested_table2 on __nested_table2.enumName = 'ЮридическоеФизическоеЛицо' and __nested_table2.orderIndex = __nested_table1.f4) as contractors";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void FilterByEnumValue()
        {
            const string sourceSql =
                @"select contractors.НаименованиеПолное as ContractorFullname
from справочник.Контрагенты as contractors
where contractors.ЮридическоеФизическоеЛицо = Значение(Перечисление.ЮридическоеФизическоеЛицо.СПокупателем)";

            const string mappings = @"Справочник.Контрагенты t1 Main
    наименованиеполное Single f1
    ЮридическоеФизическоеЛицо Single f2 Перечисление.ЮридическоеФизическоеЛицо
Перечисление.ЮридическоеФизическоеЛицо t2 Main
    ССылка Single f3
    Порядок Single f4";

            const string expectedResult = @"select
    contractors.f1 as ContractorFullname
from t1 as contractors
where contractors.f2 = (select
    __nested_table0.f3
from t2 as __nested_table0
left join simple1c__enumMappings as __nested_table1 on __nested_table1.enumName = 'ЮридическоеФизическоеЛицо' and __nested_table1.orderIndex = __nested_table0.f4
where __nested_table1.enumValueName = 'СПокупателем')";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void PatchGroupByWithEnumsText()
        {
            const string sourceSql =
                @"select ПРЕДСТАВЛЕНИЕ(contractors.ЮридическоеФизическоеЛицо) as ContractorTypeText, count(*) as ContractorCount
from справочник.Контрагенты as contractors
group by contractors.ЮридическоеФизическоеЛицо";

            const string mappings = @"Справочник.Контрагенты t1 Main
    ЮридическоеФизическоеЛицо Single f2 Перечисление.ЮридическоеФизическоеЛицо
Перечисление.ЮридическоеФизическоеЛицо t2 Main
    ССылка Single f3
    Порядок Single f4";

            const string expectedResult =
                @"select
    contractors.__nested_field0 as ContractorTypeText,
    count(*) as ContractorCount
from (select
    __nested_table2.enumValueName as __nested_field0
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.f3 = __nested_table0.f2
left join simple1c__enumMappings as __nested_table2 on __nested_table2.enumName = 'ЮридическоеФизическоеЛицо' and __nested_table2.orderIndex = __nested_table1.f4) as contractors
group by contractors.__nested_field0";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }
    }
}