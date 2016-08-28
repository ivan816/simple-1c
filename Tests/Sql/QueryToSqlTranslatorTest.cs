using System;
using System.Collections.Generic;
using NUnit.Framework;
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
            const string mappings = @"Справочник.Контрагенты t1
    ИНН c1";
            const string expectedResult = @"select contractors.c1 as CounterpartyInn
    from t1 as contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
        
        [Test]
        public void InvertIsFolder()
        {
            const string sourceSql = @"select contracts.ЭтоГруппа as IsFolder
    from справочник.ДоговорыКонтрагентов as contracts";
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1
    ЭтоГруппа c1";
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
            const string mappings = @"Справочник.Контрагенты t1
    ИНН c1";
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
            const string mappings = @"Справочник.Контрагенты t1
    ИНН c1
    ЮридическоеФизическоеЛицо c2 Перечисление.ЮридическоеФизическоеЛицо
Перечисление.ЮридическоеФизическоеЛицо t2
    Ссылка c3
    Порядок c4 ";
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
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1
    Дата c1";
            const string expectedResult = @"select date_part('year', contracts.c1) as ContractDate
    from t1 as contracts";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
        
        [Test]
        public void CanUseRussianSyntax()
        {
            const string sourceSql = @"выбрать contractors.ИНН как CounterpartyInn
    из Справочник.Контрагенты как contractors
    ГДЕ contractors.наименование =""test-name"" и contractors.ИНН <> ""test-inn""";
            const string mappings = @"Справочник.Контрагенты t1
    ИНН c1
    Наименование c2";
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
            const string mappings = @"Справочник.Контрагенты t1
    ИНН c1";
            const string expectedResult = @"select contractors.c1 as CounterpartyInn
    from t1 as Contractors";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
        
        [Test]
        public void DoNotIncludeBraceInPropertyName()
        {
            const string sourceSql = @"select (contractors.ИНН) as CounterpartyInn
    from Справочник.Контрагенты as contractors";
            const string mappings = @"Справочник.Контрагенты t1
    ИНН c1";
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
            const string mappings = @"Справочник.Контрагенты t1
    ИНН c1
    Наименование c2";
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
            const string mappings = @"Справочник.ДоговорыКонтрагентов t1
    ВалютаВзаиморасчетов c1 Справочник.Валюты
Справочник.Валюты t2
    Ссылка с2
    Наименование c3";
            const string expectedResult = @"select contracts.__nested_field0 as Currency
    from (select
    __nested_table1.c3 as __nested_field0
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.с2 = __nested_table0.c1) as contracts";
            CheckTranslate(mappings, sourceSql, expectedResult);
        }
        
        [Test]
        public void CorrectCrashForInvalidUseIfPresentationFunction()
        {
            const string sourceSql = @"select ПРЕДСТАВЛЕНИЕ(testRef.Договор) as TestContract
    from Справочник.Тестовый as testRef";
            const string mappings = @"Справочник.Тестовый t1
    Договор с1 Документ.ПоступлениеТоваровУслуг
Документ.ПоступлениеТоваровУслуг t2
    Ссылка с2
    Наименование c3";
            
            var exception = Assert.Throws<InvalidOperationException>(() => 
                CheckTranslate(mappings, sourceSql, null));
            Assert.That(exception.Message, Is.EqualTo("function [ПРЕДСТАВЛЕНИЕ] is only supported for [Перечисления,Справочники]"));
        }

        [Test]
        public void SimpleWithAlias()
        {
            const string sourceSql = @"select contractors.ИНН as CounterpartyInn
                from Справочник.Контрагенты as contractors";
            const string mappings = @"Справочник.Контрагенты T1
    ИНН C1";
            const string expectedResult = @"select contractors.C1 as CounterpartyInn
                from T1 as contractors";
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
            const string expectedResult = @"select contracts.c1 as Kind1, otherContracts.c1 as Kind2
from t1 as contracts
left outer join t1 as otherContracts";
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

            const string mappings = @"Справочник.ДоговорыКонтрагентов t1
    владелец f1 Справочник.Контрагенты
    наименование f4
Справочник.Контрагенты t2
    ССылка f2
    ИНН f3";

            const string expectedResult = @"select contracts.f4, contracts.__nested_field0 as ContractorInn
from (select
    __nested_table0.f4,
    __nested_table1.f3 as __nested_field0
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.f2 = __nested_table0.f1) as contracts";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void ManyLevelNesting()
        {
            const string sourceSql =
                @"select contracts.Наименование as ContractName,contracts.владелец.ИНН as ContractorInn,contracts.владелец.ОсновнойБанковскийСчет.НомерСчета as AccountNumber
from справочник.ДоговорыКонтрагентов as contracts";

            const string mappings = @"Справочник.ДоговорыКонтрагентов t1
    владелец f1 Справочник.Контрагенты
    наименование f2
Справочник.Контрагенты t2
    ССылка f3
    ИНН f4
    ОсновнойБанковскийСчет f5 Справочник.БанковскиеСчета
Справочник.БанковскиеСчета t3
    ССылка f6
    НомерСчета f7";

            const string expectedResult =
                @"select contracts.f2 as ContractName,contracts.__nested_field0 as ContractorInn,contracts.__nested_field1 as AccountNumber
from (select
    __nested_table0.f2,
    __nested_table1.f4 as __nested_field0,
    __nested_table2.f7 as __nested_field1
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.f3 = __nested_table0.f1
left join t3 as __nested_table2 on __nested_table2.f6 = __nested_table1.f5) as contracts";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void EnumsNoText()
        {
            const string sourceSql =
                @"select contractors.НаименованиеПолное as ContractorFullname,contractors.ЮридическоеФизическоеЛицо as ContractorType
from справочник.Контрагенты as contractors";

            const string mappings = @"Справочник.Контрагенты t1
    наименованиеполное f1
    ЮридическоеФизическоеЛицо f2 Перечисление.ЮридическоеФизическоеЛицо
Перечисление.ЮридическоеФизическоеЛицо t2
    ССылка f3
    Порядок f4";

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

            const string mappings = @"Справочник.Контрагенты t1
    наименованиеполное f1
    ЮридическоеФизическоеЛицо f2 Перечисление.ЮридическоеФизическоеЛицо
Перечисление.ЮридическоеФизическоеЛицо t2
    ССылка f3
    Порядок f4";

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

            const string mappings = @"Справочник.Контрагенты t1
    наименованиеполное f1
    ЮридическоеФизическоеЛицо f2 Перечисление.ЮридическоеФизическоеЛицо
Перечисление.ЮридическоеФизическоеЛицо t2
    ССылка f3
    Порядок f4";

            const string expectedResult =
                @"select contractors.f1 as ContractorFullname,contractors.__nested_field0 as ContractorTypeText,contractors.f2 as ContractorType
from (select
    __nested_table0.f1,
    __nested_table0.f2,
    __nested_table2.enumValueName as __nested_field0
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

            const string mappings = @"Справочник.Контрагенты t1
    ЮридическоеФизическоеЛицо f2 Перечисление.ЮридическоеФизическоеЛицо
Перечисление.ЮридическоеФизическоеЛицо t2
    ССылка f3
    Порядок f4";

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

            const string mappings = @"Справочник.Контрагенты t1
    ССылка f1
    ИНН f2
    Родитель f3 Справочник.Контрагенты
    ГоловнойКонтрагент f4 Справочник.Контрагенты";

            const string expectedResult =
                @"select contractors.f2 as Inn,contractors.__nested_field0 as ParentInn,contractors.__nested_field1 as HeadInn
from (select
    __nested_table0.f2,
    __nested_table1.f2 as __nested_field0,
    __nested_table2.f2 as __nested_field1
from t1 as __nested_table0
left join t1 as __nested_table1 on __nested_table1.f1 = __nested_table0.f3
left join t1 as __nested_table2 on __nested_table2.f1 = __nested_table0.f4) as contractors";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        [Test]
        public void ManyNestedProperties()
        {
            const string sourceSql =
                @"select contracts.владелец.ИНН as ContractorInn,contracts.владелец.Наименование as ContractorName
from справочник.ДоговорыКонтрагентов as contracts";

            const string mappings = @"Справочник.ДоговорыКонтрагентов t1
    владелец f1 Справочник.Контрагенты
Справочник.Контрагенты t2
    ССылка f2
    ИНН f3
    Наименование f4";

            const string expectedResult =
                @"select contracts.__nested_field0 as ContractorInn,contracts.__nested_field1 as ContractorName
from (select
    __nested_table1.f3 as __nested_field0,
    __nested_table1.f4 as __nested_field1
from t1 as __nested_table0
left join t2 as __nested_table1 on __nested_table1.f2 = __nested_table0.f1) as contracts";

            CheckTranslate(mappings, sourceSql, expectedResult);
        }

        private static void CheckTranslate(string mappings, string sql, string expectedTranslated)
        {
            var inmemoryMappingStore = Parse(SpacesToTabs(mappings));
            var sqlTranslator = new QueryToSqlTranslator(inmemoryMappingStore);
            var actualTranslated = sqlTranslator.Translate(sql);
            Assert.That(SpacesToTabs(actualTranslated), Is.EqualTo(SpacesToTabs(expectedTranslated)));
        }

        private static string SpacesToTabs(string s)
        {
            return s.Replace("    ", "\t");
        }

        private static InMemoryMappingStore Parse(string source)
        {
            var items = source.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            var tableMappings = new Dictionary<string, TableMapping>(StringComparer.OrdinalIgnoreCase);
            var columnMappings = new List<PropertyMapping>();
            string queryTableName = null;
            string dbTableName = null;
            foreach (var s in items)
            {
                if (s[0] == '\t')
                    columnMappings.Add(PropertyMapping.Parse(s.Substring(1)));
                else
                {
                    if (queryTableName != null)
                        tableMappings.Add(queryTableName,
                            new TableMapping(queryTableName, dbTableName, columnMappings.ToArray()));
                    var tableNames = s.Split(new[] {" "}, StringSplitOptions.None);
                    queryTableName = tableNames[0];
                    dbTableName = tableNames[1];
                    columnMappings.Clear();
                }
            }
            if (queryTableName != null)
                tableMappings.Add(queryTableName,
                    new TableMapping(queryTableName, dbTableName, columnMappings.ToArray()));
            return new InMemoryMappingStore(tableMappings);
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