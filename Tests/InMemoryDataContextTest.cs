using System.Linq;
using NUnit.Framework;
using Simple1C.Interface;
using Simple1C.Tests.Helpers;
using Simple1C.Tests.Metadata1C.Документы;
using Simple1C.Tests.Metadata1C.Справочники;

namespace Simple1C.Tests
{
    public class InMemoryDataContextTest: TestBase
    {
        private IDataContext dataContext;

        protected override void SetUp()
        {
            base.SetUp();
            dataContext = DataContextFactory.CreateInMemory(typeof (Контрагенты).Assembly);
        }

        [Test]
        public void EmptyStore()
        {
            var values = dataContext.Select<Контрагенты>().ToArray();
            Assert.That(values, Is.Empty);
        }

        [Test]
        public void SimpleCatalogSave()
        {
            var entity = new Контрагенты
            {
                Наименование = "Тестовое наименование"
            };
            dataContext.Save(entity);
            var values = dataContext.Select<Контрагенты>().ToArray();
            Assert.That(values.Length, Is.EqualTo(1));
            Assert.That(values[0], Is.SameAs(entity));
            Assert.That(values[0].Код, Is.Not.Null);
            Assert.That(values[0].Код, Is.Not.Empty);
        }

        [Test]
        public void SimpleDocumentSave()
        {
            var entity = new ПоступлениеТоваровУслуг
            {
                Комментарий = "Тестовое наименование"
            };
            dataContext.Save(entity);
            var values = dataContext.Select<ПоступлениеТоваровУслуг>().ToArray();
            Assert.That(values.Length, Is.EqualTo(1));
            Assert.That(values[0], Is.SameAs(entity));
            Assert.That(values[0].Номер, Is.Not.Null);
        }

        [Test]
        public void SimpleInnerSave()
        {
            var counterparty = new Контрагенты
            {
                Наименование = "Тестовый контрагент"
            };
            var contract = new ДоговорыКонтрагентов
            {
                Наименование = "Тестовый договор",
                Владелец = counterparty
            };

            dataContext.Save(contract);
            var array = dataContext.Select<Контрагенты>().ToArray();
            Assert.That(array.Length, Is.EqualTo(1));
            Assert.That(array[0].Наименование, Is.EqualTo("Тестовый контрагент"));
        }
    }
}