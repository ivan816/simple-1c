using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Simple1C.Impl;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Queriables;
using Simple1C.Interface;
using Simple1C.Interface.ObjectModel;
using Simple1C.Tests.Helpers;
using Simple1C.Tests.Metadata1C.Справочники;

namespace Simple1C.Tests
{
    public abstract class QueryBuilderTest : TestBase
    {
        public class Simple : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<ПрочиеДоходыИРасходы>(),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.ПрочиеДоходыИРасходы КАК src");
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class ПрочиеДоходыИРасходы
            {
                public string Наименование { get; set; }
            }
        }

        public class CanUseTake : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<ПрочиеДоходыИРасходы>().Take(GetTakeValue()),
                    "ВЫБРАТЬ ПЕРВЫЕ 17 src.Ссылка ИЗ Справочник.ПрочиеДоходыИРасходы КАК src");
            }

            private static int GetTakeValue()
            {
                return 17;
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class ПрочиеДоходыИРасходы
            {
                public string Наименование { get; set; }
            }
        }

        public class CanUseFirst : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                lastQuery = null;
                var result = Source<ПрочиеДоходыИРасходы>().FirstOrDefault();
                Assert.That(result, Is.Null);
                Assert.IsNotNull(lastQuery);
                Assert.That(lastQuery.QueryText,
                    Is.EqualTo("ВЫБРАТЬ ПЕРВЫЕ 1 src.Ссылка ИЗ Справочник.ПрочиеДоходыИРасходы КАК src"));
                Assert.That(lastQuery.Parameters, Is.Empty);
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class ПрочиеДоходыИРасходы
            {
                public string Наименование { get; set; }
            }
        }

        public class CanUseSingle : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                lastQuery = null;
                var result = Source<ПрочиеДоходыИРасходы>().SingleOrDefault();
                Assert.That(result, Is.Null);
                Assert.IsNotNull(lastQuery);
                Assert.That(lastQuery.QueryText,
                    Is.EqualTo("ВЫБРАТЬ ПЕРВЫЕ 2 src.Ссылка ИЗ Справочник.ПрочиеДоходыИРасходы КАК src"));
                Assert.That(lastQuery.Parameters, Is.Empty);
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class ПрочиеДоходыИРасходы
            {
                public string Наименование { get; set; }
            }
        }

        public class CauUseOrderBy : QueryBuilderTest
        {
            [Test]
            public void TestAsc()
            {
                AssertQuery(Source<ПрочиеДоходыИРасходы>().OrderBy(x => x.Период),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.ПрочиеДоходыИРасходы КАК src УПОРЯДОЧИТЬ ПО src.Период");
            }
            
            [Test]
            public void TestNested()
            {
                AssertQuery(Source<ПрочиеДоходыИРасходы>().OrderBy(x => x.Контрагент.Наименование),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.ПрочиеДоходыИРасходы КАК src УПОРЯДОЧИТЬ ПО src.Контрагент.Наименование");
            }

            [Test]
            public void TestDesc()
            {
                AssertQuery(Source<ПрочиеДоходыИРасходы>().OrderByDescending(x => x.Наименование),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.ПрочиеДоходыИРасходы КАК src УПОРЯДОЧИТЬ ПО src.Наименование УБЫВ");
            }

            [Test]
            public void TestMany()
            {
                AssertQuery(Source<ПрочиеДоходыИРасходы>()
                    .OrderByDescending(x => x.Наименование)
                    .ThenBy(x => x.Период),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.ПрочиеДоходыИРасходы КАК src УПОРЯДОЧИТЬ ПО src.Наименование УБЫВ,src.Период");
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class ПрочиеДоходыИРасходы
            {
                public string Наименование { get; set; }
                public DateTime Период { get; set; }
                public Контрагенты Контрагент { get; set; }
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class Контрагенты
            {
                public string Наименование { get; set; }
            }
        }

        public class BooleanFilterTest : QueryBuilderTest
        {
            [Test]
            public void Negative()
            {
                AssertQuery(Source<ФизическиеЛица>()
                    .Where(x => !x.ПометкаУдаления),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.ФизическиеЛица КАК src ГДЕ (НЕ src.ПометкаУдаления)");
            }

            [Test]
            public void Positive()
            {
                AssertQuery(Source<ФизическиеЛица>()
                    .Where(x => x.ПометкаУдаления),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.ФизическиеЛица КАК src ГДЕ src.ПометкаУдаления");
            }

            [Test]
            public void NestedPositive()
            {
                AssertQuery(Source<ФизическиеЛица>()
                    .Where(x => x.Менеджер.ПометкаУдаления),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.ФизическиеЛица КАК src ГДЕ src.Менеджер.ПометкаУдаления");
            }

            [Test]
            public void NestedBinary()
            {
                AssertQuery(Source<ФизическиеЛица>()
                    .Where(x => x.Менеджер.ПометкаУдаления && x.Имя == "test"),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.ФизическиеЛица КАК src ГДЕ (src.Менеджер.ПометкаУдаления И (src.Имя = &p0))",
                    P("p0", "test"));
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class ФизическиеЛица
            {
                public string Имя { get; set; }
                public bool ПометкаУдаления { get; set; }
                public ФизическиеЛица Менеджер { get; set; }
            }
        }

        public class ExplicitSourceName : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<ТестовыеРекизиты>("Справочник.ПрочиеДоходыИРасходы")
                    .Where(x => x.Наименование == "test-name"),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.ПрочиеДоходыИРасходы КАК src ГДЕ (src.Наименование = &p0)",
                    P("p0", "test-name"));
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class ТестовыеРекизиты
            {
                public string Наименование { get; set; }
            }
        }

        public class SimpleWithFilter : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<ПрочиеДоходыИРасходы>()
                    .Where(x => x.Наименование == "test"),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.ПрочиеДоходыИРасходы КАК src ГДЕ (src.Наименование = &p0)",
                    P("p0", "test"));
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class ПрочиеДоходыИРасходы
            {
                public string Наименование { get; set; }
            }
        }

        public class SimpleWithSeveralFilters : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<ПрочиеДоходыИРасходы>()
                    .Where(x => x.Наименование == "test")
                    .Where(x => x.Наименование == "test"),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.ПрочиеДоходыИРасходы КАК src ГДЕ ((src.Наименование = &p0) И (src.Наименование = &p1))",
                    P("p0", "test"), P("p1", "test"));
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class ПрочиеДоходыИРасходы
            {
                public string Наименование { get; set; }
            }
        }

        public class SimpleWithNestedFilter : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<ПрочиеДоходыИРасходы>()
                    .Where(x => x.Контрагент.Наименование == "test"),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.ПрочиеДоходыИРасходы КАК src ГДЕ (src.Контрагент.Наименование = &p0)",
                    P("p0", "test"));
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class ПрочиеДоходыИРасходы
            {
                public Контрагенты Контрагент { get; set; }
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class Контрагенты
            {
                public string Наименование { get; set; }
            }
        }

        public class TypeOfCatalogTest : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<БанковскиеСчета>()
                    .Where(x => x.Владелец.GetType() == typeof (Контрагенты)),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.БанковскиеСчета КАК src ГДЕ (ТИПЗНАЧЕНИЯ(src.Владелец) = ТИП(Справочник.Контрагенты))");
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class Контрагенты
            {
                public string Наименование { get; set; }
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class БанковскиеСчета
            {
                public object Владелец { get; set; }
            }
        }

        public class TypeOfStringTest : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<Контрагенты>()
                    .Where(x => x.Наименование.GetType() == typeof (string)),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.Контрагенты КАК src ГДЕ (ТИПЗНАЧЕНИЯ(src.Наименование) = ТИП(СТРОКА))");
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class Контрагенты
            {
                public object Наименование { get; set; }
            }
        }

        public class IsCatalogTest : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<БанковскиеСчета>()
                    .Where(x => x.Владелец is Контрагенты),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.БанковскиеСчета КАК src ГДЕ (ТИПЗНАЧЕНИЯ(src.Владелец) = ТИП(Справочник.Контрагенты))");
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class Контрагенты
            {
                public string Наименование { get; set; }
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class БанковскиеСчета
            {
                public object Владелец { get; set; }
            }
        }

        public class IsStringTest : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<Контрагенты>()
                    .Where(x => x.Наименование is string),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.Контрагенты КАК src ГДЕ (ТИПЗНАЧЕНИЯ(src.Наименование) = ТИП(СТРОКА))");
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class Контрагенты
            {
                public object Наименование { get; set; }
            }
        }

        public class IsNullTest : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<ДоговорыКонтрагентов>()
                    .Where(x => x.ВидДоговора != null),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.ДоговорыКонтрагентов КАК src ГДЕ (src.ВидДоговора <> ЗНАЧЕНИЕ(Перечисление.ВидыДоговоровКонтрагентов.ПустаяСсылка))");
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class ДоговорыКонтрагентов
            {
                public ВидыДоговоровКонтрагентов? ВидДоговора { get; set; }
            }

            [ConfigurationScope(ConfigurationScope.Перечисления)]
            public enum ВидыДоговоровКонтрагентов
            {
                СПоставщиком
            }
        }

        public class RegistryTest : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<ОтветственныеЛицаОрганизаций>(),
                    "ВЫБРАТЬ src.Ссылка ИЗ РегистрСведений.ОтветственныеЛицаОрганизаций КАК src");
            }

            [ConfigurationScope(ConfigurationScope.РегистрыСведений)]
            public class ОтветственныеЛицаОрганизаций
            {
                public string Наименование { get; set; }
            }
        }

        private BuiltQuery lastQuery;

        protected IQueryable<T> Source<T>(string sourceName = null)
        {
            var queryProvider = RelinqHelpers.CreateQueryProvider(new TypeMapper(typeof (Контрагенты).Assembly),
                delegate(BuiltQuery query)
                {
                    lastQuery = query;
                    return new T[0];
                });
            return new RelinqQueryable<T>(queryProvider, sourceName);
        }

        protected KeyValuePair<string, object> P(string name, object value)
        {
            return new KeyValuePair<string, object>(name, value);
        }

        protected void AssertQuery<T>(IQueryable<T> query, string expectedQueryText,
            params KeyValuePair<string, object>[] expectedParameters)
        {
            lastQuery = null;
            Assert.That(query.ToArray().Length, Is.EqualTo(0));
            Assert.IsNotNull(lastQuery);
            Assert.That(lastQuery.QueryText, Is.EqualTo(expectedQueryText));
            Assert.That(DumpParametersToString(lastQuery.Parameters),
                Is.EqualTo(DumpParametersToString(expectedParameters)));
        }

        private static string DumpParametersToString(IEnumerable<KeyValuePair<string, object>> parameters)
        {
            return parameters
                .Select(x => string.Format("{0}={1}", x.Key, x.Value))
                .JoinStrings(";");
        }
    }
}