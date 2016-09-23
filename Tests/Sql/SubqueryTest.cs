using NUnit.Framework;

namespace Simple1C.Tests.Sql
{
    public class SubqueryTest : TranslationTestBase
    {
        [Test]
        public void CanSelectAllInSubquery()
        {
            const string source = "select t.ИНН, t.Наименование from (select * from Справочник.Контрагенты) t";
            const string mappings = @"Справочник.Контрагенты contractors0 Main
    ИНН Single inn
    Наименование Single name";

            const string expected = @"select
    t.inn,
    t.name
from (select
    *
from contractors0) as t";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void JoinInsideSubquery()
        {
            const string source = @"select 
    subquery.contractName, 
    subquery.inn,
    docs.Номер
    from Справочник.Контрагенты contractorsOuter
    left join (select 
        contracts.Наименование as contractName,
        contractorsInner.ИНН as inn,
        contractorsInner.Ссылка as contractorId
    from Справочник.Контрагенты contractorsInner
    left join Справочник.ДоговорыКонтрагентов contracts on contractorsInner.Ссылка = contracts.Владелец) as subquery on subquery.contractorId = contractorsOuter.Ссылка
    left join Документ.ПоступлениеНаРасчетныйСчет docs on docs.Контрагент = subquery.contractorId";

            const string mappings = @"Справочник.ДоговорыКонтрагентов contracts1 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData
    Владелец Single contractorId Справочник.Контрагенты
Справочник.Контрагенты contractors2 Main
    Ссылка Single id
    Наименование Single name
    ИНН Single inn
    ОбластьДанныхОсновныеДанные Single mainData
Документ.ПоступлениеНаРасчетныйСчет docs3 Main
    Контрагент Single contractorId Справочник.Контрагенты
    Номер Single number
    ОбластьДанныхОсновныеДанные Single mainData";

            const string expected = @"select
    subquery.contractName,
    subquery.inn,
    docs.number
from contractors2 as contractorsOuter
left join (select
    contracts.name as contractName,
    contractorsInner.inn as inn,
    contractorsInner.id as contractorId
from contractors2 as contractorsInner
left join contracts1 as contracts on contractorsInner.mainData = contracts.mainData and contractorsInner.id = contracts.contractorId) as subquery on subquery.contractorId = contractorsOuter.id
left join docs3 as docs on docs.contractorId = subquery.contractorId";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void JoinOnSubquery()
        {
            const string source = @"
select 
    contracts.Наименование, 
    contractor.Наименование 
from (select 
        Наименование, 
        Ссылка 
    from Справочник.Контрагенты as contractors) contractor
left join Справочник.ДоговорыКонтрагентов contracts on contracts.Владелец = contractor.Ссылка ";

            const string mappings = @"Справочник.ДоговорыКонтрагентов contractsTable1 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData
    Владелец Single contractorId Справочник.Контрагенты
Справочник.Контрагенты contractorsTable2 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData";

            const string expected = @"select
    contracts.name,
    contractor.name
from (select
    contractors.name,
    contractors.id
from (select
    __nested_table0.name,
    __nested_table0.id
from contractorsTable2 as __nested_table0
where __nested_table0.mainData in (10, 200)) as contractors) as contractor
left join (select
    __nested_table1.name,
    __nested_table1.contractorId
from contractsTable1 as __nested_table1
where __nested_table1.mainData in (10, 200)) as contracts on contracts.contractorId = contractor.id";
            CheckTranslate(mappings, source, expected, 10, 200);
        }

        [Test]
        public void SelectFromSubqueryWithAreas()
        {
            const string source = "select ИНН, Наименование_Alias from " +
                                  "(select ИНН, Наименование as Наименование_Alias " +
                                  "from Справочник.Контрагенты) t";

            const string mappings = @"Справочник.Контрагенты contractors0 Main
    ИНН Single inn
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData";

            const string expected =
                @"select
    t.inn,
    t.Наименование_Alias
from (select
    __subquery0.inn,
    __subquery0.name as Наименование_Alias
from (select
    __nested_table0.inn,
    __nested_table0.name
from contractors0 as __nested_table0
where __nested_table0.mainData in (10, 20, 30)) as __subquery0) as t";
            CheckTranslate(mappings, source, expected, 10, 20, 30);
        }

        [Test]
        public void SubqueryInFilterExpressionRefersToOuterTable()
        {
            var source = @"select Номер from Документ.ПоступлениеНаРасчетныйСчет dOuter 
    where СуммаДокумента in 
    (select СуммаДокумента from Документ.ПоступлениеНаРасчетныйСчет where Номер <> dOuter.Номер)";

            const string mappings = @"Документ.ПоступлениеНаРасчетныйСчет documents1 Main
    Номер Single number
    СуммаДокумента Single sum";

            var expected = @"select
    dOuter.number
from documents1 as dOuter
where dOuter.sum in (select
    sum
from documents1
where number <> dOuter.number)";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void SubqueryUsesNestedPropertyOfOuterTable()
        {
            const string source = @"select * from Документ.ПоступлениеНаРасчетныйСчет as cOuter 
    where Контрагент.Наименование in (select Наименование from 
                    Справочник.Контрагенты cInner 
                    where cOuter.ДоговорКонтрагента.Наименование like cInner.Наименование )";
            const string mappings = @"Документ.ПоступлениеНаРасчетныйСчет documents1 Main
    Ссылка Single id
    Контрагент Single contractorId Справочник.Контрагенты
    ДоговорКонтрагента Single contractId Справочник.ДоговорыКонтрагентов
    ОбластьДанныхОсновныеДанные Single mainData
Справочник.Контрагенты contractors2 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData
Справочник.ДоговорыКонтрагентов contracts3 Main
    Ссылка Single id
    Наименование Single name
    ОбластьДанныхОсновныеДанные Single mainData";
            const string expected = @"select
    *
from (select
    __nested_table1.name as __nested_field0,
    __nested_table2.name as __nested_field1
from documents1 as __nested_table0
left join contractors2 as __nested_table1 on __nested_table1.mainData = __nested_table0.mainData and __nested_table1.id = __nested_table0.contractorId
left join contracts3 as __nested_table2 on __nested_table2.mainData = __nested_table0.mainData and __nested_table2.id = __nested_table0.contractId) as cOuter
where cOuter.__nested_field0 in (select
    cInner.name
from contractors2 as cInner
where cOuter.__nested_field1 like cInner.name)";
            CheckTranslate(mappings, source, expected);
        }

        [Test]
        public void UseSubqueryInFilterExpression()
        {
            const string source = @"select * from Документ.ПоступлениеНаРасчетныйСчет 
                            where ИННКонтрагента in (select ИНН from Справочник.Контрагенты where ИННВведенКорректно = true)";
            const string mappings = @"Справочник.Контрагенты contractors0 Main
    ИНН Single inn
    ИННВведенКорректно Single innIsCorrect
Документ.ПоступлениеНаРасчетныйСчет documents1 Main
    ИННКонтрагента Single contractorInn";

            const string expected = @"select
    *
from documents1
where contractorInn in (select
    inn
from contractors0
where innIsCorrect = true)";
            CheckTranslate(mappings, source, expected);
        }
    }
}