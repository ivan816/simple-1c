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
        
        public class Projection : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<ПрочиеДоходыИРасходы>()
                    .Select(x => new { sum = x.Сумма, quantity = x.Количество }),
                    "ВЫБРАТЬ src.Сумма КАК src_Сумма,src.Количество КАК src_Количество ИЗ Справочник.ПрочиеДоходыИРасходы КАК src");
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class ПрочиеДоходыИРасходы
            {
                public string Наименование { get; set; }
                public decimal Сумма { get; set; }
                public decimal Количество { get; set; }
            }
        }

        public class CastInProjection : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<ПоступениеНаРасчетныйСчет>()
                    .Select(x => new { Наименование = ((ICounterparty)x.Контрагент).Наименование }),
                    "ВЫБРАТЬ src.Контрагент.Наименование КАК src_Контрагент_Наименование ИЗ Документ.ПоступениеНаРасчетныйСчет КАК src");
            }

            [ConfigurationScope(ConfigurationScope.Документы)]
            public class ПоступениеНаРасчетныйСчет
            {
                public object Контрагент { get; set; }
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class Контрагенты : ICounterparty
            {
                public string Наименование { get; set; }
                public decimal Сумма { get; set; }
                public decimal Количество { get; set; }
            }

            public interface ICounterparty
            {
                string Наименование { get; set; }
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

        public class UniqueIdentifier : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<Контрагенты>()
                    .Select(x => new
                    {
                        x.Наименование,
                        x.УникальныйИдентификатор
                    }),
                    "ВЫБРАТЬ src.Наименование КАК src_Наименование,src.Ссылка КАК src_Ссылка_ИД ИЗ Справочник.Контрагенты КАК src");
            }

            [Test]
            public void QueryByParameter()
            {
                var guid = Guid.NewGuid();
                AssertQuery(Source<Контрагенты>()
                    .Where(x => x.УникальныйИдентификатор == guid)
                    .Select(x => new
                    {
                        x.Наименование
                    }),
                    "ВЫБРАТЬ src.Наименование КАК src_Наименование ИЗ Справочник.Контрагенты КАК src ГДЕ (src.Ссылка = &p0)",
                    P("p0", new ConvertUniqueIdentifierCmd
                    {
                        entityType = typeof(Контрагенты),
                        id = guid
                    }));
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class Контрагенты
            {
                public string Наименование { get; set; }
                public Guid? УникальныйИдентификатор { get; set; }
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

            [Test]
            public void GracefullCrashForInvalidOrderByExpression()
            {
                ПрочиеДоходыИРасходы value = null;
                var exception = Assert.Throws<InvalidOperationException>(() => value = Source<ПрочиеДоходыИРасходы>()
                    .OrderByDescending(x => LocalFunc(x.Наименование))
                    .Single());
                Assert.That(value, Is.Null);
                const string expectedMessage = "can't apply [OrderByDescending] operator by " +
                                               "expression [LocalFunc([x].Наименование)]." +
                                               "Expression must be a chain of member accesses.";
                Assert.That(exception.Message, Is.EqualTo(expectedMessage));
            }

            private static string LocalFunc(string s)
            {
                return s;
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

        public class LessAndGreaterFilterTest : QueryBuilderTest
        {
            [Test]
            public void GreaterThan()
            {
                AssertQuery(Source<КурсыВалют>()
                    .Where(x => x.Период > new DateTime(2016,6,24)),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.КурсыВалют КАК src ГДЕ (src.Период > &p0)",
                    P("p0", new DateTime(2016, 6, 24)));
            }

            [Test]
            public void GreaterThanOrEqual()
            {
                AssertQuery(Source<КурсыВалют>()
                    .Where(x => x.Период >= new DateTime(2016,6,24)),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.КурсыВалют КАК src ГДЕ (src.Период >= &p0)",
                    P("p0", new DateTime(2016, 6, 24)));
            }

            [Test]
            public void LessThan()
            {
                AssertQuery(Source<КурсыВалют>()
                    .Where(x => x.Период < new DateTime(2016,6,24)),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.КурсыВалют КАК src ГДЕ (src.Период < &p0)",
                    P("p0", new DateTime(2016, 6, 24)));
            }

            [Test]
            public void LessThanOrEqual()
            {
                AssertQuery(Source<КурсыВалют>()
                    .Where(x => x.Период <= new DateTime(2016,6,24)),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.КурсыВалют КАК src ГДЕ (src.Период <= &p0)",
                    P("p0", new DateTime(2016, 6, 24)));
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class КурсыВалют
            {
                public DateTime Период { get; set; }
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
        
        public class FilterByInvalidExpression : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                ПрочиеДоходыИРасходы value = null;
                var exception = Assert.Throws<InvalidOperationException>(() => value =
                    Source<ПрочиеДоходыИРасходы>()
                        .Single(x => LocalFunc(x.Наименование) == "."));
                Assert.That(value, Is.Null);
                const string expectedMessage = "can't apply 'Where' operator for " +
                                               "expression [(LocalFunc([x].Наименование) == \".\")]." +
                                               "Expression must be a chain of member accesses.";
                Assert.That(exception.Message, Is.EqualTo(expectedMessage));
            }
            
            [Test]
            public void LocalMemberChain()
            {
                ПрочиеДоходыИРасходы value = null;
                var exception = Assert.Throws<InvalidOperationException>(() => value =
                    Source<ПрочиеДоходыИРасходы>()
                        .Single(x => LocalWrapFunc(x.Наименование).s == "."));
                Assert.That(value, Is.Null);
                const string expectedMessage = "can't apply 'Where' operator for " +
                                               "expression [(LocalWrapFunc([x].Наименование).s == \".\")]." +
                                               "Expression must be a chain of member accesses.";
                Assert.That(exception.Message, Is.EqualTo(expectedMessage));
            }

            private static string LocalFunc(string s)
            {
                return s;
            }
            
            private static ValueWrap LocalWrapFunc(string s)
            {
                return new ValueWrap{s = s};
            }

            private class ValueWrap
            {
                public string s;
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

        public class CastInQuery : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<Счет>()
                    .Where(x => ((Контрагенты) x.Владелец).Наименование == "test"),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.Счет КАК src ГДЕ (src.Владелец.Наименование = &p0)",
                    P("p0", "test"));
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class Контрагенты
            {
                public string Наименование { get; set; }
            }
            
            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class Счет
            {
                public object Владелец { get; set; }
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

        public class CountTest : QueryBuilderTest
        {
            [Test]
            public void WithoutProjection()
            {
                AssertQueryCount(SourceForCount<ДоговорыКонтрагентов>(),
                    "ВЫБРАТЬ КОЛИЧЕСТВО(*) КАК src_Count ИЗ Справочник.ДоговорыКонтрагентов КАК src");
            }
            
            [Test]
            public void WithProjection()
            {
                AssertQueryCount(SourceForCount<ДоговорыКонтрагентов>().Select(x => new {x.ВидДоговора}),
                    "ВЫБРАТЬ КОЛИЧЕСТВО(*) КАК src_Count ИЗ Справочник.ДоговорыКонтрагентов КАК src");
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

        public class LikeFilterTest : QueryBuilderTest
        {
            [Test]
            public void Contains()
            {
                AssertQuery(Source<Контрагенты>()
                    .Where(x => x.Наименование.Contains("some text")),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.Контрагенты КАК src ГДЕ (src.Наименование ПОДОБНО \"%\" + &p0 + \"%\")",
                    P("p0", "some text"));
            }

            [Test]
            public void EndsWith() {
                AssertQuery(Source<Контрагенты>()
                    .Where(x => x.Наименование.EndsWith("some text")),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.Контрагенты КАК src ГДЕ (src.Наименование ПОДОБНО \"%\" + &p0)",
                    P("p0", "some text"));
            }

            [Test]
            public void StartsWith() {
                AssertQuery(Source<Контрагенты>()
                    .Where(x => x.Наименование.StartsWith("some text")),
                    "ВЫБРАТЬ src.Ссылка ИЗ Справочник.Контрагенты КАК src ГДЕ (src.Наименование ПОДОБНО &p0 + \"%\")",
                    P("p0", "some text"));
            }

            [ConfigurationScope(ConfigurationScope.Справочники)]
            public class Контрагенты
            {
                public string Наименование { get; set; }
            }
        }

        public class RegistryTest : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<ОтветственныеЛицаОрганизаций>(),
                    "ВЫБРАТЬ * ИЗ РегистрСведений.ОтветственныеЛицаОрганизаций КАК src");
            }

            [ConfigurationScope(ConfigurationScope.РегистрыСведений)]
            public class ОтветственныеЛицаОрганизаций
            {
                public string Наименование { get; set; }
            }
        }

        public class PresentationInFilterTest : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                ПоступлениеНаРасчетныйСчет value = null;
                var exception = Assert.Throws<InvalidOperationException>(() => value =
                    Source<ПоступлениеНаРасчетныйСчет>()
                        .Single(x => Функции.Представление(x.Наименование) == "."));
                Assert.That(value, Is.Null);
                const string expectedMessage = "can't apply 'Where' operator for " +
                                               "expression [(Представление([x].Наименование) == \".\")]." +
                                               "Expression must be a chain of member accesses.";
                Assert.That(exception.Message, Is.EqualTo(expectedMessage));
            }

            [ConfigurationScope(ConfigurationScope.Документы)]
            public class ПоступлениеНаРасчетныйСчет
            {
                public string Наименование { get; set; }
                public string Код { get; set; }
            }
        }

        public class PresentationTest : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<ПоступлениеНаРасчетныйСчет>().Select(x => new
                {
                    x.Код,
                    Наименование = Функции.Представление(x.Наименование)
                }),
                    "ВЫБРАТЬ src.Код КАК src_Код,ПРЕДСТАВЛЕНИЕ(src.Наименование) КАК src_Наименование_ПРЕДСТАВЛЕНИЕ ИЗ Документ.ПоступлениеНаРасчетныйСчет КАК src");
            }

            [ConfigurationScope(ConfigurationScope.Документы)]
            public class ПоступлениеНаРасчетныйСчет
            {
                public string Наименование { get; set; }
                public string Код { get; set; }
            }
        }

        public class TypeOfProjectionTest : QueryBuilderTest
        {
            [Test]
            public void Test()
            {
                AssertQuery(Source<ПоступлениеНаРасчетныйСчет>().Select(x => new
                {
                    CodeType = x.Код.GetType(),
                    Наименование = Функции.Представление(x.Наименование.GetType())
                }),
                    "ВЫБРАТЬ ТИПЗНАЧЕНИЯ(src.Код) КАК src_Код_ТИПЗНАЧЕНИЯ,ПРЕДСТАВЛЕНИЕ(ТИПЗНАЧЕНИЯ(src.Наименование)) КАК src_Наименование_ТИПЗНАЧЕНИЯ_ПРЕДСТАВЛЕНИЕ ИЗ Документ.ПоступлениеНаРасчетныйСчет КАК src");
            }

            [ConfigurationScope(ConfigurationScope.Документы)]
            public class ПоступлениеНаРасчетныйСчет
            {
                public string Наименование { get; set; }
                public string Код { get; set; }
            }
        }

        private BuiltQuery lastQuery;

        protected IQueryable<T> Source<T>(string sourceName = null)
        {
            var queryProvider = RelinqHelpers.CreateQueryProvider(new TypeRegistry(typeof (Контрагенты).Assembly),
                delegate(BuiltQuery query)
                {
                    lastQuery = query;
                    return new T[0];
                });
            return new RelinqQueryable<T>(queryProvider, sourceName);
        }

        protected IQueryable<T> SourceForCount<T>(string sourceName = null)
        {
            var queryProvider = RelinqHelpers.CreateQueryProvider(new TypeRegistry(typeof (Контрагенты).Assembly),
                delegate(BuiltQuery query)
                {
                    lastQuery = query;
                    return new int[1];
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

        protected void AssertQueryCount<T>(IQueryable<T> query, string expectedQueryText,
            params KeyValuePair<string, object>[] expectedParameters)
        {
            lastQuery = null;
            Assert.That(query.Count(), Is.EqualTo(0));
            Assert.IsNotNull(lastQuery);
            Assert.That(lastQuery.QueryText, Is.EqualTo(expectedQueryText));
            Assert.That(DumpParametersToString(lastQuery.Parameters),
                Is.EqualTo(DumpParametersToString(expectedParameters)));
        }

        private static string DumpParametersToString(IEnumerable<KeyValuePair<string, object>> parameters)
        {
            return parameters
                .Select(delegate(KeyValuePair<string, object> x)
                {
                    var value = x.Value;
                    var convertUniqueIdentifier = value as ConvertUniqueIdentifierCmd;
                    if (convertUniqueIdentifier != null)
                        value = "convert-uniqueidentifier:" + convertUniqueIdentifier.entityType.FormatName() + ":" +
                                convertUniqueIdentifier.id;
                    var convertEnum = value as ConvertEnumCmd;
                    if (convertEnum != null)
                        value = "convert-enum:" + value;
                    return string.Format("{0}={1}", x.Key, value);
                })
                .JoinStrings(";");
        }
    }
}