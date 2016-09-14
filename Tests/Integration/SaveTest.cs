using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using Simple1C.Impl.Com;
using Simple1C.Interface;
using Simple1C.Tests.Metadata1C.Перечисления;
using Simple1C.Tests.Metadata1C.Справочники;
using Simple1C.Tests.TestEntities;

namespace Simple1C.Tests.Integration
{
    internal class SaveTest : COMDataContextTestBase
    {
        [Test]
        public void CanAddRecursive()
        {
            var organization = dataContext.Single<Организации>(x => x.ИНН == organizationInn);
            var counterpartyContract = new ДоговорыКонтрагентов
            {
                ВидДоговора = ВидыДоговоровКонтрагентов.СПокупателем,
                Наименование = "test name",
                Владелец = new Контрагенты
                {
                    ИНН = "1234567890",
                    Наименование = "test-counterparty",
                    ЮридическоеФизическоеЛицо = ЮридическоеФизическоеЛицо.ЮридическоеЛицо
                },
                Организация = organization
            };
            dataContext.Save(counterpartyContract);
            Assert.That(string.IsNullOrEmpty(counterpartyContract.Код), Is.False);
            Assert.That(string.IsNullOrEmpty(counterpartyContract.Владелец.Код), Is.False);

            var valueTable = globalContext.Execute("ВЫБРАТЬ * ИЗ Справочник.ДоговорыКонтрагентов ГДЕ Код=&Code", new Dictionary<string, object>
            {
                {"Code", counterpartyContract.Код}
            }).Unload();
            Assert.That(valueTable.Count, Is.EqualTo(1));
            Assert.That(valueTable[0].GetString("Наименование"), Is.EqualTo("test name"));
        }

        [Test]
        public void CanSaveNullDateTimes()
        {
            var контрагент = new Контрагенты
            {
                Наименование = "test",
                ИНН = "123456789"
            };
            dataContext.Save(контрагент);

            var контрагент2 = dataContext.Single<Контрагенты>(x => x.Код == контрагент.Код);
            Assert.That(контрагент2.СвидетельствоДатаВыдачи, Is.Null);

            контрагент2.СвидетельствоДатаВыдачи = new DateTime(2010, 7, 18);
            dataContext.Save(контрагент2);
            контрагент2 = dataContext.Single<Контрагенты>(x => x.Код == контрагент.Код);
            Assert.That(контрагент2.СвидетельствоДатаВыдачи, Is.EqualTo(new DateTime(2010, 7, 18)));

            контрагент2.СвидетельствоДатаВыдачи = null;
            dataContext.Save(контрагент2);
            контрагент2 = dataContext.Single<Контрагенты>(x => x.Код == контрагент.Код);
            Assert.That(контрагент2.СвидетельствоДатаВыдачи, Is.Null);
        }

        [Test]
        public void RecursiveSave()
        {
            var контрагентВася = new Контрагенты
            {
                Наименование = "Василий"
            };
            контрагентВася.ГоловнойКонтрагент = контрагентВася;
            var exception = Assert.Throws<InvalidOperationException>(() => dataContext.Save(контрагентВася));
            Assert.That(exception.Message,
                Does.Contain("cycle detected for entity type [Контрагенты]: [Контрагенты->ГоловнойКонтрагент]"));

            //см. тудушку про ГоловнойКонтрагент
            //            var valueTable = globalContext.Execute("ВЫБРАТЬ * ИЗ Справочник.Контрагенты ГДЕ Код=&Code", new[]
            //            {
            //                new KeyValuePair<string, object>("Code", контрагентВася.Код)
            //            });
            //
            //            Assert.That(valueTable.Count, Is.EqualTo(1));
            //            Assert.That(valueTable[0]["Наименование"], Is.EqualTo("Василий"));
            //            Assert.That(ComHelpers.GetProperty(valueTable[0]["ГоловнойКонтрагент"],
            //                "Наименование"), Is.EqualTo("Василий"));
        }

        [Test]
        public void CanSaveNewEntityWithoutAnyPropertiesSet()
        {
            var контрагент = new Контрагенты();
            dataContext.Save(контрагент);

            Assert.That(string.IsNullOrEmpty(контрагент.Код), Is.False);
            var counterpartyTable = globalContext.Execute("ВЫБРАТЬ * ИЗ Справочник.Контрагенты ГДЕ Код=&Code",
                new Dictionary<string, object>
                {
                    {"Code", контрагент.Код}
                }).Unload();
            Assert.That(counterpartyTable.Count, Is.EqualTo(1));
        }

        [Test]
        public void ChangeMustBeStrongerThanTracking()
        {
            var counterpart = new Counterpart
            {
                Inn = "7711223344",
                Kpp = "771101001",
                FullName = "Test counterparty",
                Name = "Test counterparty"
            };
            dynamic counterpartyAccessObject = testObjectsManager.CreateCounterparty(counterpart);
            var counterpartContractAccessObject = testObjectsManager.CreateCounterpartContract(counterpartyAccessObject.Ссылка,
                new CounterpartyContract
                {
                    CurrencyCode = "643",
                    Name = "test-contract",
                    Kind = CounterpartContractKind.Incoming
                });
            string counterpartyContractCode = counterpartContractAccessObject.Code;

            var contract = dataContext.Select<ДоговорыКонтрагентов>()
                .Single(x => x.Код == counterpartyContractCode);
            if (contract.Владелец.ИНН == "7711223344")
            {
                contract.Владелец.ИНН = "7711223345";
                contract.Владелец = new Контрагенты
                {
                    ИНН = "7711223355",
                    Наименование = "Test counterparty 2",
                    НаименованиеПолное = "Test counterparty 2"
                };
            }
            dataContext.Save(contract);

            var valueTable = globalContext.Execute("ВЫБРАТЬ * ИЗ Справочник.ДоговорыКонтрагентов ГДЕ Код=&Code",
                new Dictionary<string, object>
                {
                    {"Code", contract.Код}
                }).Unload();
            Assert.That(valueTable.Count, Is.EqualTo(1));
            Assert.That(ComHelpers.GetProperty(valueTable[0]["Владелец"], "Наименование"), Is.EqualTo("Test counterparty 2"));
        }

        [Test]
        public void CanUnpostDocument()
        {
            var поступлениеТоваровУслуг = CreateFullFilledDocument();
            поступлениеТоваровУслуг.Проведен = true;
            dataContext.Save(поступлениеТоваровУслуг);

            поступлениеТоваровУслуг.Услуги[0].Содержание = "каре";
            dataContext.Save(поступлениеТоваровУслуг);

            var document = GetDocumentByNumber(поступлениеТоваровУслуг.Номер);
            Assert.That(document.Проведен, Is.True);
            Assert.That(document.Услуги.Получить(0).Содержание, Is.EqualTo("каре"));
        }

        [Test]
        public void CanPostDocuments()
        {
            var поступлениеТоваровУслуг = CreateFullFilledDocument();
            dataContext.Save(поступлениеТоваровУслуг);
            Assert.That(GetDocumentByNumber(поступлениеТоваровУслуг.Номер).Проведен, Is.False);
            поступлениеТоваровУслуг.Проведен = true;
            dataContext.Save(поступлениеТоваровУслуг);
            Assert.That(GetDocumentByNumber(поступлениеТоваровУслуг.Номер).Проведен);
        }

        [Test]
        public void CanSaveMultipleEntitiesAtOnce()
        {
            dataContext.Save(new object[]
            {
                new Контрагенты {Наименование = "test-contragent"},
                new СтатьиЗатрат {Наименование = "test-cost-item"}
            });
            Assert.That(CountOf<Контрагенты>(x => x.Наименование == "test-contragent"), Is.EqualTo(1));
            Assert.That(CountOf<СтатьиЗатрат>(x => x.Наименование == "test-cost-item"), Is.EqualTo(1));
        }

        private int CountOf<T>(Expression<Func<T, bool>> filter )
        {
            return dataContext.Select<T>()
                .Where(filter)
                .AsEnumerable()
                .Count();
        }

        [Test]
        public void ModifyReference()
        {
            var counterpart = new Counterpart
            {
                Inn = "7711223344",
                Kpp = "771101001",
                FullName = "Test counterparty",
                Name = "Test counterparty"
            };
            dynamic counterpartyAccessObject = testObjectsManager.CreateCounterparty(counterpart);
            var counterpartContractAccessObject = testObjectsManager.CreateCounterpartContract(counterpartyAccessObject.Ссылка,
                new CounterpartyContract
                {
                    Name = "test-counterparty-contract",
                    Kind = CounterpartContractKind.Incoming,
                    CurrencyCode = "643"
                });
            string initialCounterpartyContractVersion = counterpartContractAccessObject.DataVersion;
            string counterpartyContractCode = counterpartContractAccessObject.Code;
            var contract = dataContext.Select<ДоговорыКонтрагентов>()
                .Single(x => x.Код == counterpartyContractCode);
            contract.Владелец.ИНН = "7711223344";
            contract.Владелец.Наименование = "Test counterparty 2";
            contract.Владелец.НаименованиеПолное = "Test counterparty 2";
            dataContext.Save(contract);

            var counterpartyTable = globalContext.Execute("ВЫБРАТЬ * ИЗ Справочник.Контрагенты ГДЕ ИНН=&Inn",
                new Dictionary<string, object>
                {
                    {"Inn", "7711223344"}
                }).Unload();
            Assert.That(counterpartyTable.Count, Is.EqualTo(1));
            Assert.That(counterpartyTable[0].GetString("Наименование"), Is.EqualTo("Test counterparty 2"));

            var counterpartyContractTable = globalContext.Execute("ВЫБРАТЬ Ссылка ИЗ Справочник.ДоговорыКонтрагентов ГДЕ Код=&Code",
                new Dictionary<string, object>
                {
                    {"Code", counterpartyContractCode}
                }).Unload();
            Assert.That(counterpartyContractTable.Count, Is.EqualTo(1));

            var comObj = counterpartyContractTable[0]["Ссылка"];
            var dataVersion = ((dynamic)comObj).DataVersion;
            Assert.That(dataVersion, Is.EqualTo(initialCounterpartyContractVersion));
        }

        [Test]
        public void GroupsCanSave()
        {
            var номенклатура = new Номенклатура
            {
                Наименование = "Товары",
                ЭтоГруппа = true
            };
            dataContext.Save(номенклатура);
            var номенклатуры = dataContext.Single<Номенклатура>(x => x.УникальныйИдентификатор == номенклатура.УникальныйИдентификатор);
            Assert.That(номенклатуры.ЭтоГруппа);
        }
    }
}