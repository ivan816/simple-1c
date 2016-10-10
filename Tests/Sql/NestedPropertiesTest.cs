using System;
using NUnit.Framework;

namespace Simple1C.Tests.Sql
{
    public class NestedPropertiesTest : TranslationTestBase
    {
        [Test]
        public void InvalidPropertyName_CorrectException()
        {
            const string sourceSql =
                @"select payments.Наименование as ContractorName
from документ.СписаниеСРасчетногоСчета as payments";

            const string mappings = @"Документ.СписаниеСРасчетногоСчета t1 Main
    ОбластьДанныхОсновныеДанные Single d1";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                CheckTranslate(mappings, sourceSql, null));
            const string expectedMessage = "no properties found for [payments.Наименование]";
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));
        }

        [Test]
        public void InvertIsFolder()
        {
            const string sourceSql = @"select contracts.этогруппа as IsFolder
    from справочник.ДоговорыКонтрагентов as contracts";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    ЭтоГруппа Single c1";
            const string expectedResult = @"select
    contracts.__nested_field0 as IsFolder
from (select
    not(__nested_table0.c1) as __nested_field0
from t1 as __nested_table0) as contracts";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void ManyInstancesOfSameProperty()
        {
            const string sourceSql =
                @"select contractors.ИНН as Inn,contractors.Родитель.ИНН as ParentInn,contractors.ГоловнойКонтрагент.ИНН as HeadInn
from справочник.Контрагенты as contractors";

            const string mappings = @"Справочник.Контрагенты t1 Main
    ССылка Single f1
    ИНН Single f2
    Родитель Single f3 Справочник.Контрагенты
    ГоловнойКонтрагент Single f4 Справочник.Контрагенты
    ОбластьДанныхОсновныеДанные Single d1";

            const string expectedResult =
                @"select
    contractors.f2 as Inn,
    contractors.__nested_field0 as ParentInn,
    contractors.__nested_field1 as HeadInn
from (select
    __nested_table0.f2,
    __nested_table1.f2 as __nested_field0,
    __nested_table2.f2 as __nested_field1
from t1 as __nested_table0
left join t1 as __nested_table1 on __nested_table1.d1 = __nested_table0.d1 and __nested_table1.f1 = __nested_table0.f3
left join t1 as __nested_table2 on __nested_table2.d1 = __nested_table0.d1 and __nested_table2.f1 = __nested_table0.f4) as contractors";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void ManyLevelNesting()
        {
            const string sourceSql =
                @"select contracts.Наименование as ContractName,contracts.владелец.ИНН as ContractorInn,contracts.владелец.ОсновнойБанковскийСчет.НомерСчета as AccountNumber
from справочник.ДоговорыКонтрагентов as contracts";

            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    владелец Single f1 Справочник.Контрагенты
    наименование Single f2
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Контрагенты t2 Main
    ССылка Single f3
    ИНН Single f4
    ОсновнойБанковскийСчет Single f5 Справочник.БанковскиеСчета
    ОбластьДанныхОсновныеДанные Single d2
Справочник.БанковскиеСчета t3 Main
    ССылка Single f6
    НомерСчета Single f7
    ОбластьДанныхОсновныеДанные Single d3";

            const string expectedResult =
                @"select
    contracts.f2 as ContractName,
    contracts.__nested_field0 as ContractorInn,
    contracts.__nested_field1 as AccountNumber
from (select
    __nested_table0.f2,
    __nested_table1.f4 as __nested_field0,
    __nested_table2.f7 as __nested_field1
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.d2 = __nested_table0.d1 and __nested_table1.f3 = __nested_table0.f1
left join t3 as __nested_table2 on __nested_table2.d3 = __nested_table1.d2 and __nested_table2.f6 = __nested_table1.f5) as contracts";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void ManyNestedProperties()
        {
            const string sourceSql =
                @"select contracts.владелец.ИНН as ContractorInn,contracts.владелец.Наименование as ContractorName
from справочник.ДоговорыКонтрагентов as contracts";

            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    владелец Single f1 Справочник.Контрагенты
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Контрагенты t2 Main
    ССылка Single f2
    ИНН Single f3
    Наименование Single f4
    ОбластьДанныхОсновныеДанные Single d2";

            const string expectedResult =
                @"select
    contracts.__nested_field0 as ContractorInn,
    contracts.__nested_field1 as ContractorName
from (select
    __nested_table1.f3 as __nested_field0,
    __nested_table1.f4 as __nested_field1
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.d2 = __nested_table0.d1 and __nested_table1.f2 = __nested_table0.f1) as contracts";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void NoPropertiesFoundForUnionReferences_CorrectException()
        {
            const string sourceSql =
                @"select payments.Контрагент.Наименование as ContractorName
from документ.СписаниеСРасчетногоСчета as payments";

            const string mappings = @"Документ.СписаниеСРасчетногоСчета t1 Main
    Контрагент UnionReferences f1_type f1_tableIndex f1_ref Справочник.Контрагенты Справочник.ФизическиеЛица
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Контрагенты t210 Main
    ССылка Single f2
    ОбластьДанныхОсновныеДанные Single d2
Справочник.ФизическиеЛица t312 Main
    ССылка Single f4
    ОбластьДанныхОсновныеДанные Single d3";

            var exception = Assert.Throws<InvalidOperationException>(() =>
                CheckTranslate(mappings, sourceSql, null));
            const string expectedMessage =
                "property [Контрагент] in [payments.Контрагент.Наименование] has multiple types [Справочник.Контрагенты,Справочник.ФизическиеЛица] " +
                "and none of them has property [Наименование]";
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));
        }

        [Test]
        public void RewriteNestedPropertyAccessToJoin()
        {
            const string sourceSql = @"select contracts.наименование, contracts.владелец.ИНН as ContractorInn
from справочник.ДоговорыКонтрагентов as contracts";

            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    владелец Single f1 Справочник.Контрагенты
    наименование Single f4
    ОбластьДанныхОсновныеДанные Single d2
Справочник.Контрагенты t2 Main
    ССылка Single f2
    ИНН Single f3
    ОбластьДанныхОсновныеДанные Single d1";

            const string expectedResult = @"select
    contracts.f4 as наименование,
    contracts.__nested_field0 as ContractorInn
from (select
    __nested_table0.f4,
    __nested_table1.f3 as __nested_field0
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.d1 = __nested_table0.d2 and __nested_table1.f2 = __nested_table0.f1) as contracts";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void SimpleSelfReference()
        {
            const string sourceSql =
                @"select contractors.Ссылка.ИНН as CounterpartyInn,contractors.ссылка as CounterpartyReference
    from Справочник.Контрагенты as contractors";
            const string mappings = @"Справочник.Контрагенты t1 Main
    Ссылка Single f1
    ИНН Single f2";
            const string expectedResult = @"select
    contractors.__nested_field0 as CounterpartyInn,
    contractors.f1 as CounterpartyReference
from (select
    __nested_table0.f2 as __nested_field0,
    __nested_table0.f1
from t1 as __nested_table0) as contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void TableSectionsUsingReference()
        {
            const string sourceSql =
                @"select docItems.номенклатура.наименование as name
from Документ.ПоступлениеТоваровУслуг.Услуги as docItems
where docItems.сСыЛка.ПометкаУдаления = false";

            const string mappings = @"Документ.ПоступлениеТоваровУслуг.Услуги t1 TableSection
    Ссылка Single f1
    номенклатура Single f2 Справочник.Номенклатура
    ОбластьДанныхОсновныеДанные Single f3
Справочник.Номенклатура t2 Main
    Ссылка Single f4
    наименование Single f56
    ОбластьДанныхОсновныеДанные Single f5
Документ.ПоступлениеТоваровУслуг t3 Main
    ССылка Single f6
    ПометкаУдаления Single f7
    ОбластьДанныхОсновныеДанные Single f8";

            const string expectedResult =
                @"select
    docItems.__nested_field0 as name
from (select
    __nested_table1.f56 as __nested_field0,
    __nested_table2.f7 as __nested_field1
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.f5 = __nested_table0.f3 and __nested_table1.f4 = __nested_table0.f2
left join t3 as __nested_table2 on __nested_table2.f8 = __nested_table0.f3 and __nested_table2.f6 = __nested_table0.f1) as docItems
where docItems.__nested_field1 = false";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void UnionReferences()
        {
            const string sourceSql =
                @"select payments.Контрагент.Наименование as ContractorName
from документ.СписаниеСРасчетногоСчета as payments";

            const string mappings = @"Документ.СписаниеСРасчетногоСчета t1 Main
    Контрагент UnionReferences f1_type f1_tableIndex f1_ref Справочник.Контрагенты Справочник.ФизическиеЛица
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Контрагенты t210 Main
    ССылка Single f2
    Наименование Single f3
    ОбластьДанныхОсновныеДанные Single d2
Справочник.ФизическиеЛица t312 Main
    ССылка Single f4
    Наименование Single f5
    ОбластьДанныхОсновныеДанные Single d3";

            const string expectedResult =
                @"select
    payments.__nested_field0 as ContractorName
from (select
    case
    when __nested_table0.f1_type = E'\\x08' and __nested_table0.f1_tableIndex = E'\\x000000D2' then __nested_table1.f3
    when __nested_table0.f1_type = E'\\x08' and __nested_table0.f1_tableIndex = E'\\x00000138' then __nested_table2.f5
end as __nested_field0
from t1 as __nested_table0
left join t210 as __nested_table1 on __nested_table1.d2 = __nested_table0.d1 and __nested_table0.f1_type = E'\\x08' and __nested_table0.f1_tableIndex = E'\\x000000D2' and __nested_table1.f2 = __nested_table0.f1_ref
left join t312 as __nested_table2 on __nested_table2.d3 = __nested_table0.d1 and __nested_table0.f1_type = E'\\x08' and __nested_table0.f1_tableIndex = E'\\x00000138' and __nested_table2.f4 = __nested_table0.f1_ref) as payments";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }
        
        [Test]
        public void DeduceEntityTypeFromIsReferenceExpression()
        {
            const string sourceSql =
                @"select payments.Контрагент.Наименование as ContractorName
from документ.СписаниеСРасчетногоСчета as payments
where payments.Контрагент.Наименование = ""test-name"" and payments.Контрагент ссылка Справочник.Контрагенты";

            const string mappings = @"Документ.СписаниеСРасчетногоСчета t1 Main
    Контрагент UnionReferences f1_type f1_tableIndex f1_ref Справочник.Контрагенты Справочник.ФизическиеЛица
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Контрагенты t210 Main
    ССылка Single f2
    Наименование Single f3
    ОбластьДанныхОсновныеДанные Single d2
Справочник.ФизическиеЛица t312 Main
    ССылка Single f4
    Наименование Single f5
    ОбластьДанныхОсновныеДанные Single d3";

            const string expectedResult =
                @"select
    payments.__nested_field1 as ContractorName
from (select
    __nested_table1.f3 as __nested_field1,
    __nested_table0.f1_type = E'\\x08' and __nested_table0.f1_tableIndex = E'\\x000000D2' as __nested_field0
from t1 as __nested_table0
left join t210 as __nested_table1 on __nested_table1.d2 = __nested_table0.d1 and __nested_table0.f1_type = E'\\x08' and __nested_table0.f1_tableIndex = E'\\x000000D2' and __nested_table1.f2 = __nested_table0.f1_ref) as payments
where payments.__nested_field1 = 'test-name' and payments.__nested_field0";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void UnionReferencesForNestedField()
        {
            const string sourceSql =
                @"select payments.Контрагент.Владелец as ContractorName
from документ.СписаниеСРасчетногоСчета as payments";

            const string mappings = @"Документ.СписаниеСРасчетногоСчета t1 Main
    Контрагент Single f1 Справочник.Контрагенты
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Контрагенты t210 Main
    ССылка Single f2
    Владелец UnionReferences f1_type f1_tableIndex f1_ref Справочник.Контрагенты Справочник.ФизическиеЛица
    ОбластьДанныхОсновныеДанные Single d2
Справочник.ФизическиеЛица t312 Main
    ССылка Single f4
    Наименование Single f5
    ОбластьДанныхОсновныеДанные Single d3";

            const string expectedResult =
                @"select
    payments.__nested_field0 as ContractorName
from (select
    __nested_table1.f1_ref as __nested_field0
from t1 as __nested_table0
left join t210 as __nested_table1 on __nested_table1.d2 = __nested_table0.d1 and __nested_table1.f2 = __nested_table0.f1) as payments";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void UnionReferencesForTopField()
        {
            const string sourceSql =
                @"select payments.Контрагент as Contractor
from документ.СписаниеСРасчетногоСчета as payments";

            const string mappings = @"Документ.СписаниеСРасчетногоСчета t1 Main
    Контрагент UnionReferences f1_type f1_tableIndex f1_ref Справочник.Контрагенты Справочник.ФизическиеЛица
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Контрагенты t210 Main
    ССылка Single f2
    ОбластьДанныхОсновныеДанные Single d2
Справочник.ФизическиеЛица t312 Main
    ССылка Single f4
    ОбластьДанныхОсновныеДанные Single d3";

            const string expectedResult =
                @"select
    payments.f1_ref as Contractor
from t1 as payments";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void UnionReferencesStripJoinsWhenFieldDoesNotExist()
        {
            const string sourceSql =
                @"select payments.Контрагент.Наименование as ContractorName
from документ.СписаниеСРасчетногоСчета as payments";

            const string mappings = @"Документ.СписаниеСРасчетногоСчета t1 Main
    Контрагент UnionReferences f1_type f1_tableIndex f1_ref Справочник.Тестовый Справочник.Контрагенты Справочник.ФизическиеЛица
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Контрагенты t210 Main
    ССылка Single f2
    Наименование Single f3
    ОбластьДанныхОсновныеДанные Single d2
Справочник.ФизическиеЛица t312 Main
    ССылка Single f4
    Наименование Single f5
    ОбластьДанныхОсновныеДанные Single d3
Справочник.Тестовый t111 Main
    ССылка Single f6
    ОбластьДанныхОсновныеДанные Single d4";

            const string expectedResult =
                @"select
    payments.__nested_field0 as ContractorName
from (select
    case
    when __nested_table0.f1_type = E'\\x08' and __nested_table0.f1_tableIndex = E'\\x000000D2' then __nested_table1.f3
    when __nested_table0.f1_type = E'\\x08' and __nested_table0.f1_tableIndex = E'\\x00000138' then __nested_table2.f5
end as __nested_field0
from t1 as __nested_table0
left join t210 as __nested_table1 on __nested_table1.d2 = __nested_table0.d1 and __nested_table0.f1_type = E'\\x08' and __nested_table0.f1_tableIndex = E'\\x000000D2' and __nested_table1.f2 = __nested_table0.f1_ref
left join t312 as __nested_table2 on __nested_table2.d3 = __nested_table0.d1 and __nested_table0.f1_type = E'\\x08' and __nested_table0.f1_tableIndex = E'\\x00000138' and __nested_table2.f4 = __nested_table0.f1_ref) as payments";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void UnionReferencesWithRepresentation()
        {
            const string sourceSql =
                @"select ПРЕДСТАВЛЕНИЕ(payments.Контрагент) as ContractorName
from документ.СписаниеСРасчетногоСчета as payments";

            const string mappings = @"Документ.СписаниеСРасчетногоСчета t1 Main
    Контрагент UnionReferences f1_type f1_tableIndex f1_ref Справочник.Контрагенты Справочник.ФизическиеЛица
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Контрагенты t210 Main
    ССылка Single f2
    Наименование Single f3
    ОбластьДанныхОсновныеДанные Single d2
Справочник.ФизическиеЛица t312 Main
    ССылка Single f4
    Наименование Single f5
    ОбластьДанныхОсновныеДанные Single d3";

            const string expectedResult =
                @"select
    payments.__nested_field0 as ContractorName
from (select
    case
    when __nested_table0.f1_type = E'\\x08' and __nested_table0.f1_tableIndex = E'\\x000000D2' then __nested_table1.f3
    when __nested_table0.f1_type = E'\\x08' and __nested_table0.f1_tableIndex = E'\\x00000138' then __nested_table2.f5
end as __nested_field0
from t1 as __nested_table0
left join t210 as __nested_table1 on __nested_table1.d2 = __nested_table0.d1 and __nested_table0.f1_type = E'\\x08' and __nested_table0.f1_tableIndex = E'\\x000000D2' and __nested_table1.f2 = __nested_table0.f1_ref
left join t312 as __nested_table2 on __nested_table2.d3 = __nested_table0.d1 and __nested_table0.f1_type = E'\\x08' and __nested_table0.f1_tableIndex = E'\\x00000138' and __nested_table2.f4 = __nested_table0.f1_ref) as payments";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }
    }
}