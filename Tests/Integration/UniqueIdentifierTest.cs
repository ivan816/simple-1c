using System;
using System.Linq;
using NUnit.Framework;
using Simple1C.Interface;
using Simple1C.Tests.Metadata1C.Справочники;

namespace Simple1C.Tests.Integration
{
    internal class UniqueIdentifierTest : COMDataContextTestBase
    {
        [Test]
        public void CanSearchByRef()
        {
            var контрагент = new Контрагенты
            {
                Наименование = "test contractor name",
                ИНН = "test-inn"
            };
            dataContext.Save(контрагент);

            var контрагент2 = dataContext.Select<Контрагенты>().Single(x => x == контрагент);
            Assert.That(контрагент2.Наименование, Is.EqualTo("test contractor name"));
            Assert.That(контрагент2.ИНН, Is.EqualTo("test-inn"));
        }

        [Test]
        public void CanSaveUniqueIdentifier()
        {
            var контрагент = new Контрагенты
            {
                Наименование = "test contractor name",
                ИНН = "test-inn"
            };
            Assert.That(контрагент.УникальныйИдентификатор, Is.Null);
            dataContext.Save(контрагент);
            Assert.That(контрагент.УникальныйИдентификатор, Is.Not.Null);
            Assert.That(контрагент.УникальныйИдентификатор, Is.Not.EqualTo(Guid.Empty));

            var контрагент2 = dataContext.Single<Контрагенты>(x => x.ИНН == контрагент.ИНН);
            Assert.That(контрагент2.УникальныйИдентификатор, Is.EqualTo(контрагент.УникальныйИдентификатор));
        }

        [Test]
        public void CanSelectUniqueIdentifier()
        {
            var контрагент1 = new Контрагенты
            {
                Наименование = "test contractor name1",
                ИНН = "test-inn1"
            };
            dataContext.Save(контрагент1);
            var контрагент2 = new Контрагенты
            {
                Наименование = "test contractor name2",
                ИНН = "test-inn2"
            };
            dataContext.Save(контрагент2);

            var контрагенты = dataContext.Select<Контрагенты>()
                .Where(x => x.ИНН == контрагент1.ИНН || x.ИНН == контрагент2.ИНН)
                .OrderByDescending(x => x.Наименование)
                .Select(x => new
                {
                    x.УникальныйИдентификатор,
                    x.Наименование
                })
                .ToArray();
            Assert.That(контрагенты.Length, Is.EqualTo(2));
            Assert.That(контрагенты[0].УникальныйИдентификатор, Is.EqualTo(контрагент2.УникальныйИдентификатор));
            Assert.That(контрагенты[0].Наименование, Is.EqualTo(контрагент2.Наименование));
            Assert.That(контрагенты[1].УникальныйИдентификатор, Is.EqualTo(контрагент1.УникальныйИдентификатор));
            Assert.That(контрагенты[1].Наименование, Is.EqualTo(контрагент1.Наименование));
        }

        [Test]
        public void CanQueryByUniqueIdentifier()
        {
            var контрагент1 = new Контрагенты
            {
                Наименование = "test contractor name1",
                ИНН = "test-inn1"
            };
            dataContext.Save(контрагент1);

            var контрагент2 = new Контрагенты
            {
                Наименование = "test contractor name2",
                ИНН = "test-inn2"
            };
            dataContext.Save(контрагент2);

            var контрагент3 =
                dataContext.Single<Контрагенты>(x => x.УникальныйИдентификатор == контрагент2.УникальныйИдентификатор);
            Assert.That(контрагент3.Наименование, Is.EqualTo("test contractor name2"));
        }
    }
}