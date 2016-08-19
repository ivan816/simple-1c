using System;
using System.Linq;
using NUnit.Framework;
using Simple1C.Impl;
using Simple1C.Interface;
using Simple1C.Tests.Helpers;
using Simple1C.Tests.Metadata1C.Справочники;

namespace Simple1C.Tests.Integration
{
    internal class DataContextManagementTest : TestBase
    {
        private GlobalContext globalContext;
        private IDataContext dataContext;

        protected override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            OpenContext(true);
        }

        [Test]
        public void CachedProjectionsDoNotCloseOverGlobalContext()
        {
            var контрагент1 = new Контрагенты
            {
                Наименование = "test-shortname1",
                ИНН = "test-inn1",
                КПП = "test-kpp",
                Комментарий = "test-comment1"
            };
            dataContext.Save(контрагент1);

            var контрагент2 = dataContext.Select<Контрагенты>()
                .Where(x => x.Наименование == "test-shortname1")
                .Select(x => new
                {
                    id = x.УникальныйИдентификатор
                })
                .SingleOrDefault();
            Assert.IsNotNull(контрагент2);
            Assert.That(контрагент2.id, Is.Not.Null);
            Assert.That(контрагент2.id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(контрагент2.id, Is.EqualTo(контрагент1.УникальныйИдентификатор));

            OpenContext(false);
            var контрагент3 = dataContext.Select<Контрагенты>()
                .Where(x => x.Наименование == "test-shortname1")
                .Select(x => new
                {
                    id = x.УникальныйИдентификатор
                })
                .SingleOrDefault();
            Assert.IsNotNull(контрагент3);
            Assert.That(контрагент3.id, Is.Not.Null);
            Assert.That(контрагент3.id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(контрагент3.id, Is.EqualTo(контрагент1.УникальныйИдентификатор));
        }

        private void OpenContext(bool resetData)
        {
            globalContext = Testing1CConnector.GetTempGlobalContext(resetData);
            dataContext = DataContextFactory.CreateCOM(globalContext.ComObject(), typeof(Контрагенты).Assembly);
        }
    }
}