using NUnit.Framework;

namespace Simple1C.Tests.Sql
{
    public class FunctionReplacementTest : TranslationTestBase
    {
        [Test]
        public void PatchBeginOfPeriodFunction()
        {
            const string sourceSql =
                @"select НачалоПериода(Дата, Месяц), НачалоПериода(ДАТАВРЕМЯ(2016,1,1), Квартал) from Документ.ПоступлениеНаРасчетныйСчет";
            const string mappings = @"Документ.ПоступлениеНаРасчетныйСчет documents1 Main
    Дата Single date";
            const string expectedResult = @"select
    date_trunc('Month', date),
    date_trunc('Quarter', cast('2016-01-01' as date))
from documents1";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void PatchDateTimeFunction()
        {
            const string sourceSql = @"select *
    from Справочник.ДоговорыКонтрагентов as contracts
    where contracts.Дата >= ДАТАВРЕМЯ(2010, 7, 10)";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    Дата Single c1";
            const string expectedResult = @"select
    *
from t1 as contracts
where contracts.c1 >= cast('2010-07-10' as date)";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void PatchIsNullFunction()
        {
            const string sourceSql = @"select ISNULL(КПП,""Нет КПП"") as kpp
    from Справочник.Контрагенты";
            const string mappings = @"Справочник.Контрагенты contracts Main
    КПП Single kpp";
            const string expectedResult = @"select
    case
    when kpp is null then 'Нет КПП'
    else kpp
end as kpp
from contracts";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void PatchQuarterFunction()
        {
            const string sourceSql = @"select КВАРТАЛ(contracts.Дата) as ContractDate
    from Справочник.ДоговорыКонтрагентов as contracts";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    Дата Single c1";
            const string expectedResult = @"select
    date_part('quarter', contracts.c1) as ContractDate
from t1 as contracts";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void PatchSubstringFunction()
        {
            const string sourceSql = @"select substring(КПП, 2, 5) from Справочник.Контрагенты";
            const string mappings = @"Справочник.Контрагенты contractors1 Main
    КПП Single kpp";
            const string expectedResult = @"select
    substring(cast(kpp as varchar), 2, 5)
from contractors1";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void PatchYearFunction()
        {
            const string sourceSql = @"select ГОД(contracts.Дата) as ContractDate
    from Справочник.ДоговорыКонтрагентов as contracts";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    Дата Single c1";
            const string expectedResult = @"select
    date_part('year', contracts.c1) as ContractDate
from t1 as contracts";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void AllowArbitraryPostgreSqlFunction()
        {
            const string sourceSql = @"select simple1c.to_guid(Ссылка), length(КПП), now() from Справочник.Контрагенты";
            const string mappings = @"Справочник.Контрагенты contractors1 Main
    КПП Single kpp
    Ссылка Single ref";

            const string expectedResult = @"select
    simple1c.to_guid(ref),
    length(kpp),
    now()
from contractors1";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
    }
}