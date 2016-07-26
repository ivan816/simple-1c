using System.Linq;
using NUnit.Framework;
using Simple1C.Tests.Metadata1C.ПланыВидовХарактеристик;
using Simple1C.Tests.Metadata1C.Справочники;

namespace Simple1C.Tests.Integration
{
    internal class СharacteristicsTest : COMDataContextTestBase
    {
        [Test]
        public void Test()
        {
            var разделыДатЗапредаИзменений = dataContext.Select<РазделыДатЗапретаИзменения>()
                .ToArray();
            Assert.That(разделыДатЗапредаИзменений.Length, Is.EqualTo(1));
            var item = разделыДатЗапредаИзменений[0];
            Assert.That(item.Наименование, Is.EqualTo("Бухгалтерский учет"));
            Assert.That(item.ТипЗначения, Is.EquivalentTo(new[] {typeof(Организации)}));
        }
    }
}