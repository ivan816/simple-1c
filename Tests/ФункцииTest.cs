using System;
using NUnit.Framework;
using Simple1C.Interface;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Tests
{
    public class ФункцииTest
    {
        [Test] 
        public void NullPresentation()
        {
            Assert.That(Функции.Представление(null), Is.EqualTo(""));
        }

        [Test] 
        public void IntPresentation()
        {
            Assert.That(Функции.Представление(1), Is.EqualTo("1"));
        }

        [Test] 
        public void BytePresentation()
        {
            Assert.That(Функции.Представление((byte)1), Is.EqualTo("1"));
        }

        [Test] 
        public void SBytePresentation()
        {
            Assert.That(Функции.Представление((sbyte)1), Is.EqualTo("1"));
        }

        [Test] 
        public void ShortPresentation()
        {
            Assert.That(Функции.Представление((short)1), Is.EqualTo("1"));
        }

        [Test] 
        public void UshortPresentation()
        {
            Assert.That(Функции.Представление((ushort)1), Is.EqualTo("1"));
        }

        [Test] 
        public void UintPresentation()
        {
            Assert.That(Функции.Представление((uint)1), Is.EqualTo("1"));
        }

        [Test] 
        public void LongPresentation()
        {
            Assert.That(Функции.Представление((long)1), Is.EqualTo("1"));
        }

        [Test] 
        public void UlongPresentation()
        {
            Assert.That(Функции.Представление((ulong)1), Is.EqualTo("1"));
        }

        [Test] 
        public void FloatPresentation()
        {
            Assert.That(Функции.Представление((float)1.4), Is.EqualTo("1,4"));
        }

        [Test] 
        public void DoublePresentation()
        {
            Assert.That(Функции.Представление((double)1.4), Is.EqualTo("1,4"));
        }

        [Test] 
        public void DecimalPresentation()
        {
            Assert.That(Функции.Представление((decimal)1.4), Is.EqualTo("1,4"));
        }

        [Test] 
        public void TruePresentation()
        {
            Assert.That(Функции.Представление(true), Is.EqualTo("Да"));
        }

        [Test] 
        public void FalsePresentation()
        {
            Assert.That(Функции.Представление(false), Is.EqualTo("Нет"));
        }

        [Test] 
        public void StringPresentation()
        {
            var str = Guid.NewGuid().ToString();
            Assert.That(Функции.Представление(str), Is.EqualTo(str));
        }

        [Test] 
        public void DateTimePresentation()
        {
            Assert.That(Функции.Представление(new DateTime(2016, 7, 25, 5, 3, 2)), Is.EqualTo("25.07.2016 5:03:02"));
        }

        [Test] 
        public void NullableDateTimePresentation()
        {
            Assert.That(Функции.Представление((DateTime?)new DateTime(2016, 7, 25, 5, 3, 2)), Is.EqualTo("25.07.2016 5:03:02"));
        }

        [Test] 
        public void GuidPresentation()
        {
            var guid = Guid.NewGuid();
            Assert.That(Функции.Представление(guid), Is.EqualTo(guid.ToString()));
        }

        [Test] 
        public void EnumPresentation()
        {
            var enumeration = TestEnumeration.EnumValue;
            Assert.That(Функции.Представление(enumeration), Is.EqualTo("EnumValueSynonym"));
        }

        [Test] 
        public void CatalogPresentation()
        {
            var description = Guid.NewGuid().ToString();
            var catalog = new TestCatalog
            {
                Наименование = description
            };
            Assert.That(Функции.Представление(catalog), Is.EqualTo(description));
        }

        [Test] 
        public void DocumentPresentation()
        {
            var catalog = new ПоступлениеНаРасчетныйСчет
            {
                Дата = new DateTime(2016, 7, 23, 17, 25, 42),
                Номер = "0000-000001"
            };
            Assert.That(Функции.Представление(catalog),
                Is.EqualTo("Поступление на расчетный счет 0000-000001 от 23.07.2016 17:25:42"));
        }

        [Test] 
        public void PlanOfAccountsPresentation()
        {
            var catalog = new Хозрасчетный
            {
                Код = "76.27.2"
            };
            Assert.That(Функции.Представление(catalog),
                Is.EqualTo("76.27.2"));
        }

        [Test]
        public void NumberTypePresentation()
        {
            Assert.That(Функции.Представление(typeof(int)), Is.EqualTo("Число"));
            Assert.That(Функции.Представление(typeof(long)), Is.EqualTo("Число"));
            Assert.That(Функции.Представление(typeof(decimal)), Is.EqualTo("Число"));
            Assert.That(Функции.Представление(typeof(sbyte)), Is.EqualTo("Число"));
            Assert.That(Функции.Представление(typeof(byte)), Is.EqualTo("Число"));
            Assert.That(Функции.Представление(typeof(short)), Is.EqualTo("Число"));
            Assert.That(Функции.Представление(typeof(ushort)), Is.EqualTo("Число"));
            Assert.That(Функции.Представление(typeof(uint)), Is.EqualTo("Число"));
            Assert.That(Функции.Представление(typeof(ulong)), Is.EqualTo("Число"));
            Assert.That(Функции.Представление(typeof(float)), Is.EqualTo("Число"));
            Assert.That(Функции.Представление(typeof(double)), Is.EqualTo("Число"));
        }

        [Test]
        public void StringTypePresentation()
        {
            Assert.That(Функции.Представление(typeof(string)), Is.EqualTo("Строка"));
        }

        [Test]
        public void BoolTypePresentation()
        {
            Assert.That(Функции.Представление(typeof(bool)), Is.EqualTo("Булево"));
        }

        [Test]
        public void GuidTypePresentation()
        {
            Assert.That(Функции.Представление(typeof(Guid)), Is.EqualTo("УникальныйИдентификатор"));
        }

        [Test]
        public void DateTimeTypePresentation()
        {
            Assert.That(Функции.Представление(typeof(DateTime)), Is.EqualTo("Дата"));
        }

        [Test]
        public void NullableTypePresentation()
        {
            Assert.That(Функции.Представление(typeof(DateTime?)), Is.EqualTo("Дата"));
        }

        [Test]
        public void CatalogTypePresentation()
        {
            Assert.That(Функции.Представление(typeof(TestCatalog)), Is.EqualTo("TestCatalogObjectPresentation"));
        }

        [ConfigurationScope(ConfigurationScope.Перечисления)]
        private enum TestEnumeration
        {
            [Synonym("EnumValueSynonym")]
            EnumValue
        }

        [ConfigurationScope(ConfigurationScope.Справочники)]
        [ObjectPresentation("TestCatalogObjectPresentation")]
        private class TestCatalog
        {
            public string Наименование { get; set; }
        }

        [ConfigurationScope(ConfigurationScope.Документы)]
        [Synonym("Поступление на расчетный счет")]
        private class ПоступлениеНаРасчетныйСчет
        {
            public string Номер { get; set; }
            public DateTime Дата { get; set; }
        }

        [ConfigurationScope(ConfigurationScope.ПланыСчетов)]
        private class Хозрасчетный
        {
            public string Код { get; set; }
        }
    }
}