using NUnit.Framework;

namespace Simple1C.Tests.Sql
{
    public class LiteralsTest : TranslationTestBase
    {
        [Test]
        public void BoolLiteral()
        {
            const string sourceSql = @"select *
    from справочник.ДоговорыКонтрагентов as contracts
    where contracts.этогруппа = ложь";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    ЭтоГруппа Single c1";
            const string expectedResult = @"select
    *
from (select
    not(__nested_table0.c1) as __nested_field0
from t1 as __nested_table0) as contracts
where contracts.__nested_field0 = false";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void EscapeSingleQuotesInStringLiterals()
        {
            const string sourceSql = "select * from Справочник.Контрагенты where ИНН <> \"123'456\"";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1";
            const string expected = @"select
    *
from t1
where c1 <> '123''456'";
            CheckTranslate(mappings, sourceSql, expected);
        }

        [Test]
        public void ConvertDoubleQuotesToSingleQuotes()
        {
            const string sourceSql = @"select contractors.ИНН as CounterpartyInn
    from Справочник.Контрагенты as contractors
    where contractors.Наименование = ""test-name""";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1
    Наименование Single c2";
            const string expectedResult = @"select
    contractors.c1 as CounterpartyInn
from t1 as contractors
where contractors.c2 = 'test-name'";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
    }
}