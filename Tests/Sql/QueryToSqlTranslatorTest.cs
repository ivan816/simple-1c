using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql;
using Simple1C.Tests.Helpers;

namespace Simple1C.Tests.Sql
{
    public class QueryToSqlTranslatorTest : TestBase
    {
        [Test]
        public void Simple()
        {
            const string sourceSql = @"select contractors.ИНН as CounterpartyInn
    from Справочник.Контрагенты as contractors";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1";
            const string expectedResult = @"select contractors.c1 as CounterpartyInn
    from t1 as contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
        
        [Test]
        public void SimpleWithAreas()
        {
            const string sourceSql = @"select contractors.ИНН as CounterpartyInn
    from Справочник.Контрагенты as contractors";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1
    ОбластьДанныхОсновныеДанные Single c2";
            const string expectedResult = @"select contractors.c1 as CounterpartyInn
    from (select
    __nested_table0.c1
from t1 as __nested_table0
where __nested_table0.c2 in (10,200)) as contractors";
            CheckTranslate(mappings, sourceSql, expectedResult, 10, 200);
        }

        [Test]
        public void SimpleSelfReference()
        {
            const string sourceSql = @"select contractors.Ссылка.ИНН as CounterpartyInn,contractors.Ссылка as CounterpartyReference
    from Справочник.Контрагенты as contractors";
            const string mappings = @"Справочник.Контрагенты t1 Main
    Ссылка Single f1
    ИНН Single f2";
            const string expectedResult = @"select contractors.__nested_field0 as CounterpartyInn,contractors.f1 as CounterpartyReference
    from (select
    __nested_table0.f2 as __nested_field0,
    __nested_table0.f1
from t1 as __nested_table0) as contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
        
        [Test]
        public void InvertIsFolder()
        {
            const string sourceSql = @"select contracts.ЭтоГруппа as IsFolder
    from справочник.ДоговорыКонтрагентов as contracts";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    ЭтоГруппа Single c1";
            const string expectedResult = @"select contracts.__nested_field0 as IsFolder
    from (select
    not(__nested_table0.c1) as __nested_field0
from t1 as __nested_table0) as contracts";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
        
        [Test]
        public void StripPresentationFunctionFromSimpleProperties()
        {
            const string sourceSql = @"select ПРЕДСТАВЛЕНИЕ(contractors.ИНН) as CounterpartyInn
    from Справочник.Контрагенты as contractors";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1";
            const string expectedResult = @"select contractors.c1 as CounterpartyInn
    from t1 as contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void UnionAll()
        {
            const string sourceSql = @"select ПРЕДСТАВЛЕНИЕ(contractors.ЮридическоеФизическоеЛицо) as Type
from Справочник.Контрагенты as contractors
where contractors.ИНН = ""test-inn1""

union all

select ПРЕДСТАВЛЕНИЕ(contractors.ЮридическоеФизическоеЛицо) as Type
from Справочник.Контрагенты as contractors
where contractors.ИНН = ""test-inn2""

union

select ПРЕДСТАВЛЕНИЕ(contractors.ЮридическоеФизическоеЛицо) as Type
from Справочник.Контрагенты as contractors
where contractors.ИНН = ""test-inn3""";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1
    ЮридическоеФизическоеЛицо Single c2 Перечисление.ЮридическоеФизическоеЛицо
Перечисление.ЮридическоеФизическоеЛицо t2 Main
    Ссылка Single c3
    Порядок Single c4 ";
            const string expectedResult = @"select contractors.__nested_field0 as Type
from (select
    __nested_table2.enumValueName as __nested_field0,
    __nested_table0.c1
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.c3 = __nested_table0.c2
left join simple1c__enumMappings as __nested_table2 on __nested_table2.enumName = 'ЮридическоеФизическоеЛицо' and __nested_table2.orderIndex = __nested_table1.c4) as contractors
where contractors.c1 = 'test-inn1'

union all

select contractors.__nested_field0 as Type
from (select
    __nested_table2.enumValueName as __nested_field0,
    __nested_table0.c1
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.c3 = __nested_table0.c2
left join simple1c__enumMappings as __nested_table2 on __nested_table2.enumName = 'ЮридическоеФизическоеЛицо' and __nested_table2.orderIndex = __nested_table1.c4) as contractors
where contractors.c1 = 'test-inn2'

union

select contractors.__nested_field0 as Type
from (select
    __nested_table2.enumValueName as __nested_field0,
    __nested_table0.c1
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.c3 = __nested_table0.c2
left join simple1c__enumMappings as __nested_table2 on __nested_table2.enumName = 'ЮридическоеФизическоеЛицо' and __nested_table2.orderIndex = __nested_table1.c4) as contractors
where contractors.c1 = 'test-inn3'";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void PatchYearFunction()
        {
            const string sourceSql = @"select ГОД(contracts.Дата) as ContractDate
    from Справочник.ДоговорыКонтрагентов as contracts";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    Дата Single c1";
            const string expectedResult = @"select date_part('year', contracts.c1) as ContractDate
    from t1 as contracts";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
        
//TODO!!
//        [Test]
//        public void PatchDateTimeFunction()
//        {
//            const string sourceSql = @"select *
//    from Справочник.ДоговорыКонтрагентов as contracts
//    where c1 >= ДАТАВРЕМЯ()";
//            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
//    Дата c1";
//            const string expectedResult = @"select date_part('year', contracts.c1) as ContractDate
//    from t1 as contracts";
//            CheckTranslate(mappings, sourceSql, expectedResult);
//        }

        [Test]
        public void CanUseRussianSyntax()
        {
            const string sourceSql = @"выбрать contractors.ИНН как CounterpartyInn
    из Справочник.Контрагенты как contractors
    ГДЕ contractors.наименование =""test-name"" и contractors.ИНН <> ""test-inn""";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1
    Наименование Single c2";
            const string expectedResult = @"select contractors.c1 as CounterpartyInn
    from t1 as contractors
    where contractors.c2 ='test-name' and contractors.c1 <> 'test-inn'";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
        
        [Test]
        public void MatchEntityAliasCaseInsensitive()
        {
            const string sourceSql = @"select contractors.ИНН as CounterpartyInn
    from Справочник.Контрагенты as Contractors";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1";
            const string expectedResult = @"select contractors.c1 as CounterpartyInn
    from t1 as Contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
        
        [Test]
        public void DoNotIncludeBraceInPropertyName()
        {
            const string sourceSql = @"select (contractors.ИНН) as CounterpartyInn
    from Справочник.Контрагенты as contractors";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1";
            const string expectedResult = @"select (contractors.c1) as CounterpartyInn
    from t1 as contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
        
        [Test]
        public void DoNotIncludeEqualSignInPropertyName()
        {
            const string sourceSql = @"select contractors.Наименование as CounterpartyInn
    from Справочник.Контрагенты as contractors
    where contractors.ИНН=""test-inn""";
            const string mappings = @"Справочник.Контрагенты t1 Main
    ИНН Single c1
    Наименование Single c2";
            const string expectedResult = @"select contractors.c2 as CounterpartyInn
    from t1 as contractors
    where contractors.c1='test-inn'";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void MapPresentationToDescriptionForReferences()
        {
            const string sourceSql = @"select ПРЕДСТАВЛЕНИЕ(contracts.ВалютаВзаиморасчетов) as Currency
    from Справочник.ДоговорыКонтрагентов as contracts";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1 Main
    ВалютаВзаиморасчетов Single c1 Справочник.Валюты
    ОбластьДанныхОсновныеДанные Single d1
Справочник.Валюты t2 Main
    Ссылка Single с2
    Наименование Single c3
    ОбластьДанныхОсновныеДанные Single d2";
            const string expectedResult = @"select contracts.__nested_field0 as Currency
    from (select
    __nested_table1.c3 as __nested_field0
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.d2 = __nested_table0.d1 and __nested_table1.с2 = __nested_table0.c1) as contracts";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
        
        [Test]
        public void CorrectCrashForInvalidUseOfPresentationFunction()
        {
            const string sourceSql = @"select ПРЕДСТАВЛЕНИЕ(testRef.Договор) as TestContract
    from Справочник.Тестовый as testRef";
            const string mappings = @"Справочник.Тестовый t1 Main
    Договор Single с1 Документ.ПоступлениеТоваровУслуг
Документ.ПоступлениеТоваровУслуг t2 Main
    Ссылка Single с2
    Наименование Single c3";
            
            var exception = Assert.Throws<InvalidOperationException>(() => 
                CheckTranslate(mappings, sourceSql, null));
            Assert.That(exception.Message, Is.EqualTo("function [ПРЕДСТАВЛЕНИЕ] is only supported for [Перечисления,Справочники]"));
        }

        [Test]
        public void AddAreaToJoin()
        {
            const string sourceSql = @"выбрать contractors.Наименование as ContractorName из
Справочник.Контрагенты as contractors
left join Справочник.КонтактныеЛица as contacts on contractors.ОсновноеКонтактноеЛицо = contacts.Ссылка";
            const string mappings = @"Справочник.Контрагенты t1 Main
    Наименование Single c1
    ОбластьДанныхОсновныеДанные Single c2
    ОсновноеКонтактноеЛицо Single c3
Справочник.КонтактныеЛица t2 Main
    Ссылка Single c4
    ОбластьДанныхОсновныеДанные Single c5";
            const string expectedResult = @"select contractors.c1 as ContractorName from t1 as contractors
left join t2 as contacts on contractors.c2 = contacts.c5 and contractors.c3 = contacts.c4";
            CheckTranslate(mappings, sourceSql, expectedResult);
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
            const string expectedResult = @"select contractors.c1 as CounterpartyInn
    from t1 as contractors
    where contractors.c2 = 'test-name'";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void Nested()
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

            const string expectedResult = @"select contracts.f4, contracts.__nested_field0 as ContractorInn
from (select
    __nested_table0.f4,
    __nested_table1.f3 as __nested_field0
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.d1 = __nested_table0.d2 and __nested_table1.f2 = __nested_table0.f1) as contracts";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void TableSectionsUsingReference()
        {
            const string sourceSql =
                @"select docItems.номенклатура.наименование as name
from Документ.ПоступлениеТоваровУслуг.Услуги as docItems
where docItems.Ссылка.ПометкаУдаления = false";

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
                @"select docItems.__nested_field0 as name
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
                @"select contracts.f2 as ContractName,contracts.__nested_field0 as ContractorInn,contracts.__nested_field1 as AccountNumber
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

            const string expectedResult = @"select contractors.f1 as ContractorFullname,contractors.f2 as ContractorType
from t1 as contractors";

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

            const string expectedResult = @"select contractors.f1 as ContractorFullname
from t1 as contractors
where contractors.f2 = (select
    __nested_table0.f3
from t2 as __nested_table0
left join simple1c__enumMappings as __nested_table1 on __nested_table1.enumName = 'ЮридическоеФизическоеЛицо' and __nested_table1.orderIndex = __nested_table0.f4
where __nested_table1.enumValueName = 'СПокупателем')";

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
                @"select contractors.f1 as ContractorFullname,contractors.__nested_field0 as ContractorTypeText,contractors.f2 as ContractorType
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
                @"select contractors.__nested_field0 as ContractorTypeText, count(*) as ContractorCount
from (select
    __nested_table2.enumValueName as __nested_field0
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.f3 = __nested_table0.f2
left join simple1c__enumMappings as __nested_table2 on __nested_table2.enumName = 'ЮридическоеФизическоеЛицо' and __nested_table2.orderIndex = __nested_table1.f4) as contractors
group by contractors.__nested_field0";

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
                @"select contractors.f2 as Inn,contractors.__nested_field0 as ParentInn,contractors.__nested_field1 as HeadInn
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
                @"select contracts.__nested_field0 as ContractorInn,contracts.__nested_field1 as ContractorName
from (select
    __nested_table1.f3 as __nested_field0,
    __nested_table1.f4 as __nested_field1
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.d2 = __nested_table0.d1 and __nested_table1.f2 = __nested_table0.f1) as contracts";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        private static void CheckTranslate(string mappings, string sql, string expectedTranslated, params int[] areas)
        {
            var inmemoryMappingStore = Parse(SpacesToTabs(mappings));
            var sqlTranslator = new QueryToSqlTranslator(inmemoryMappingStore, areas);
            var actualTranslated = sqlTranslator.Translate(sql);
            Assert.That(SpacesToTabs(actualTranslated), Is.EqualTo(SpacesToTabs(expectedTranslated)));
        }

        private static string SpacesToTabs(string s)
        {
            return s.Replace("    ", "\t");
        }

        private static InMemoryMappingStore Parse(string source)
        {
            var tableMappings = StringHelpers.ParseLinesWithTabs(source, delegate(string s, List<string> list)
            {
                var tableNames = s.Split(new[] {" "}, StringSplitOptions.None);
                return new TableMapping(tableNames[0], tableNames[1],
                    TableMapping.ParseTableType(tableNames[2]),
                    list.Select(PropertyMapping.Parse).ToArray());
            });
            return new InMemoryMappingStore(tableMappings.ToDictionary(x => x.QueryTableName,
                StringComparer.OrdinalIgnoreCase));
        }

        internal class InMemoryMappingStore : IMappingSource
        {
            private readonly Dictionary<string, TableMapping> mappings;

            public InMemoryMappingStore(Dictionary<string, TableMapping> mappings)
            {
                this.mappings = mappings;
            }

            public TableMapping ResolveTable(string queryName)
            {
                return mappings[queryName];
            }
        }
    }
}