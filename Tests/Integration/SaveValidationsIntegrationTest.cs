using System;
using NUnit.Framework;
using Simple1C.Tests.Metadata1C.Константы;
using Simple1C.Tests.Metadata1C.Справочники;

namespace Simple1C.Tests.Integration
{
    internal class SaveValidationsIntegrationTest : COMDataContextTestBase
    {
        [Test]
        public void CrashOnMaxStringLengthViolation()
        {
            var номенклатура = new Номенклатура {Наименование = new string('x', 101)};

            var exception = Assert.Throws<InvalidOperationException>(() => dataContext.Save(номенклатура));

            const string expectedMessageFormat =
                "[Справочник.Номенклатура.Наименование] value [{0}] length [101] " +
                "is greater than configured max [100]";
            Assert.That(exception.Message, Is.EqualTo(string.Format(expectedMessageFormat,
                new string('x', 101))));
        }

        [Test]
        public void CrashOnMaxStringLengthViolationForNonCodeAndName()
        {
            var номенклатура = new Номенклатура
            {
                Наименование = "test-name",
                Артикул = new string('x', 26)
            };
            var exception = Assert.Throws<InvalidOperationException>(() => dataContext.Save(номенклатура));

            const string expectedMessageFormat =
                "[Справочник.Номенклатура.Артикул] value [{0}] length [26] " +
                "is greater than configured max [25]";
            Assert.That(exception.Message, Is.EqualTo(string.Format(expectedMessageFormat,
                new string('x', 26))));
        }

        [Test]
        public void CrashOnMaxStringLengthViolationForConstants()
        {
            var номенклатура = new АдресЦКК
            {
                Значение = new string('x', 301)
            };
            var exception = Assert.Throws<InvalidOperationException>(() => dataContext.Save(номенклатура));

            const string expectedMessageFormat =
                "[Константа.АдресЦКК] value [{0}] length [301] " +
                "is greater than configured max [300]";
            Assert.That(exception.Message, Is.EqualTo(string.Format(expectedMessageFormat,
                new string('x', 301))));
        }
    }
}