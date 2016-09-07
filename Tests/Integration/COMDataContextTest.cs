using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Simple1C.Impl.Com;
using Simple1C.Interface;
using Simple1C.Tests.Metadata1C.Документы;
using Simple1C.Tests.Metadata1C.Перечисления;
using Simple1C.Tests.Metadata1C.Справочники;
using Simple1C.Tests.TestEntities;

namespace Simple1C.Tests.Integration
{
    internal class COMDataContextTest : COMDataContextTestBase
    {
        [Test]
        public void CheckAmbigousAssembly()
        {
            var exception =
                Assert.Throws<InvalidOperationException>(
                    () => DataContextFactory.CreateCOM(globalContext.ComObject(), Assembly.GetExecutingAssembly()));
            const string expectedMessageFormat =
                "can't map [{0}] to [Simple1C.Tests] because it's already mapped to [Metadata1C]";
            Assert.That(exception.Message,
                Is.EqualTo(string.Format(expectedMessageFormat, globalContext.GetConnectionString())));
        }

        [Test]
        public void Simple()
        {
            testObjectsManager.CreateCounterparty(new Counterpart
            {
                Name = "test-name",
                Inn = "1234567890",
                Kpp = "123456789"
            });
            var instance = dataContext
                .Select<Контрагенты>()
                .Single(x => x.ИНН == "1234567890");
            Assert.That(instance.Наименование, Is.EqualTo("test-name"));
            Assert.That(instance.ИНН, Is.EqualTo("1234567890"));
            Assert.That(instance.КПП, Is.EqualTo("123456789"));
        }

        [Test]
        public void SelectWithRef()
        {
            var counterpart = new Counterpart
            {
                Name = "test-counterpart-name",
                Inn = "0987654321",
                Kpp = "987654321"
            };
            dynamic counterpartAccessObject = testObjectsManager.CreateCounterparty(counterpart);
            var bankAccountAccessObject = testObjectsManager.CreateBankAccount(counterpartAccessObject.Ссылка,
                new BankAccount
                {
                    Bank = new Bank
                    {
                        Bik = Banks.AlfaBankBik
                    },
                    Number = "40702810001111122222",
                    CurrencyCode = "643"
                });

            counterpartAccessObject.ОсновнойБанковскийСчет = bankAccountAccessObject.Ссылка;
            counterpartAccessObject.Write();

            var counterpartyContractAccessObject =
                testObjectsManager.CreateCounterpartContract(counterpartAccessObject.Ссылка, new CounterpartyContract
                {
                    CurrencyCode = "643",
                    Name = "Валюта",
                    Kind = CounterpartContractKind.OutgoingWithAgency
                });
            string counterpartContractCode = counterpartyContractAccessObject.Код;

            var contractFromStore = dataContext
                .Select<ДоговорыКонтрагентов>()
                .Single(x => x.Код == counterpartContractCode);

            Assert.That(contractFromStore.Владелец.ИНН, Is.EqualTo("0987654321"));
            Assert.That(contractFromStore.Владелец.КПП, Is.EqualTo("987654321"));
            Assert.That(contractFromStore.Владелец.Наименование, Is.EqualTo("test-counterpart-name"));
            Assert.That(contractFromStore.Владелец.ОсновнойБанковскийСчет.НомерСчета,
                Is.EqualTo("40702810001111122222"));
            Assert.That(contractFromStore.Владелец.ОсновнойБанковскийСчет.Владелец,
                Is.TypeOf<Контрагенты>());
            Assert.That(((Контрагенты) contractFromStore.Владелец.ОсновнойБанковскийСчет.Владелец)
                .ИНН,
                Is.EqualTo("0987654321"));
            Assert.That(contractFromStore.ВидДоговора, Is.EqualTo(ВидыДоговоровКонтрагентов.СКомиссионеромНаЗакупку));
        }

        [Test]
        public void QueryWithRefAccess()
        {
            var counterpart = new Counterpart
            {
                Name = "test-counterpart-name",
                Inn = "0987654321",
                Kpp = "987654321"
            };
            dynamic counterpartAccessObject = testObjectsManager.CreateCounterparty(counterpart);
            dynamic bankAccountAccessObject = testObjectsManager.CreateBankAccount(counterpartAccessObject.Ссылка,
                new BankAccount
                {
                    Bank = new Bank
                    {
                        Bik = Banks.AlfaBankBik
                    },
                    Number = "40702810001111122222",
                    CurrencyCode = "643"
                });

            counterpartAccessObject.ОсновнойБанковскийСчет = bankAccountAccessObject.Ссылка;
            counterpartAccessObject.Write();

            testObjectsManager.CreateCounterpartContract(counterpartAccessObject.Ссылка, new CounterpartyContract
            {
                CurrencyCode = "643",
                Name = "Валюта",
                Kind = CounterpartContractKind.OutgoingWithAgency
            });

            var contractFromStore = dataContext.Select<ДоговорыКонтрагентов>()
                .Single(x => x.Владелец.ОсновнойБанковскийСчет.НомерСчета == "40702810001111122222");
            Assert.That(contractFromStore.Наименование, Is.EqualTo("Валюта"));
        }

        [Test]
        public void QueryWithObject()
        {
            var counterpart = new Counterpart
            {
                Name = "test-counterpart-name",
                Inn = "0987654321",
                Kpp = "987654321"
            };
            dynamic counterpartAccessObject = testObjectsManager.CreateCounterparty(counterpart);
            testObjectsManager.CreateBankAccount(counterpartAccessObject.Ссылка,
                new BankAccount
                {
                    Bank = new Bank
                    {
                        Bik = Banks.AlfaBankBik
                    },
                    Number = "40702810001111122222",
                    CurrencyCode = "643"
                });

            var account = dataContext.Select<БанковскиеСчета>()
                .Single(x => x.Владелец is Контрагенты);
            Assert.That(account.ВалютаДенежныхСредств.Код, Is.EqualTo("643"));
            Assert.That(account.Владелец, Is.TypeOf<Контрагенты>());
            Assert.That(((Контрагенты) account.Владелец).ИНН, Is.EqualTo("0987654321"));
            Assert.That(((Контрагенты) account.Владелец).КПП, Is.EqualTo("987654321"));
        }

        [Test]
        public void NullableEnumCanSet()
        {
            var counterpart = new Counterpart
            {
                Name = "test-counterpart-name",
                Inn = "0987654321",
                Kpp = "987654321"
            };
            dynamic counterpartAccessObject = testObjectsManager.CreateCounterparty(counterpart);
            testObjectsManager.CreateBankAccount(counterpartAccessObject.Ссылка,
                new BankAccount
                {
                    Bank = new Bank
                    {
                        Bik = Banks.AlfaBankBik
                    },
                    Number = "40702810001111122222",
                    CurrencyCode = "643"
                });
            testObjectsManager.CreateCounterpartContract(counterpartAccessObject.Ссылка, new CounterpartyContract
            {
                CurrencyCode = "643",
                Name = "Валюта",
                Kind = CounterpartContractKind.OutgoingWithAgency
            });
            string counterpartyCode = counterpartAccessObject.Код;

            var contracts = dataContext.Select<ДоговорыКонтрагентов>()
                .Where(x => x.Владелец.Код == counterpartyCode)
                .ToArray();
            Assert.That(contracts.Length, Is.EqualTo(1));
            Assert.That(contracts[0].ВидДоговора, Is.EqualTo(ВидыДоговоровКонтрагентов.СКомиссионеромНаЗакупку));
        }

        [Test]
        public void EnumParameterMapping()
        {
            var counterpart = new Counterpart
            {
                Name = "test-counterpart-name",
                Inn = "0987654321",
                Kpp = "987654321"
            };
            dynamic counterpartAccessObject = testObjectsManager.CreateCounterparty(counterpart);
            testObjectsManager.CreateBankAccount(counterpartAccessObject.Ссылка,
                new BankAccount
                {
                    Bank = new Bank
                    {
                        Bik = Banks.AlfaBankBik
                    },
                    Number = "40702810001111122222",
                    CurrencyCode = "643"
                });
            testObjectsManager.CreateCounterpartContract(counterpartAccessObject.Ссылка, new CounterpartyContract
            {
                CurrencyCode = "643",
                Name = "Валюта",
                Kind = CounterpartContractKind.OutgoingWithAgency
            });

            var contracts = dataContext.Select<ДоговорыКонтрагентов>()
                .Where(x => x.ВидДоговора == ВидыДоговоровКонтрагентов.СКомиссионеромНаЗакупку)
                .ToArray();

            Assert.That(contracts.Length, Is.EqualTo(1));
            Assert.That(contracts[0].ВидДоговора, Is.EqualTo(ВидыДоговоровКонтрагентов.СКомиссионеромНаЗакупку));
        }

        [Test]
        public void CanFilterForEmptyReference()
        {
            dynamic counterpartAccessObject = CreateTestCounterpart();
            var counterpartyContractAccessObject =
                testObjectsManager.CreateCounterpartContract(counterpartAccessObject.Ссылка,
                    new CounterpartyContract
                    {
                        Name = "test-description",
                        Kind = CounterpartContractKind.Others
                    });
            string counterpartyContractCode = counterpartyContractAccessObject.Код;

            var contracts = dataContext.Select<ДоговорыКонтрагентов>()
                .Where(x => x.Код == counterpartyContractCode)
                .Where(x => x.ТипЦен == null)
                .ToArray();
            Assert.That(contracts.Length, Is.EqualTo(1));
            Assert.That(contracts[0].Код, Is.EqualTo(counterpartyContractCode));
            Assert.That(contracts[0].ТипЦен, Is.Null);

            contracts = dataContext.Select<ДоговорыКонтрагентов>()
                .Where(x => x.Код == counterpartyContractCode)
                .Where(x => x.ТипЦен != null)
                .ToArray();
            Assert.That(contracts.Length, Is.EqualTo(0));

            contracts = dataContext.Select<ДоговорыКонтрагентов>()
                .Where(x => counterpartyContractCode == x.Код)
                .Where(x => null == x.ТипЦен)
                .ToArray();
            Assert.That(contracts.Length, Is.EqualTo(1));
            Assert.That(contracts[0].Код, Is.EqualTo(counterpartyContractCode));
            Assert.That(contracts[0].ТипЦен, Is.Null);
        }

        [Test]
        public void CanAddCounterparty()
        {
            var counterparty = new Контрагенты
            {
                ИНН = "1234567890",
                Наименование = "test-counterparty",
                ЮридическоеФизическоеЛицо = ЮридическоеФизическоеЛицо.ЮридическоеЛицо
            };
            dataContext.Save(counterparty);
            Assert.That(string.IsNullOrEmpty(counterparty.Код), Is.False);

            var valueTable = globalContext.Execute("ВЫБРАТЬ * ИЗ Справочник.Контрагенты ГДЕ Код=&Code",
                new Dictionary<string, object>
                {
                    {"Code", counterparty.Код}
                }).Unload();
            Assert.That(valueTable.Count, Is.EqualTo(1));
            Assert.That(valueTable[0].GetString("ИНН"), Is.EqualTo("1234567890"));
            Assert.That(valueTable[0].GetString("Наименование"), Is.EqualTo("test-counterparty"));

            dynamic comObject = globalContext.ComObject();
            var enumsDispatchObject = comObject.Перечисления.ЮридическоеФизическоеЛицо;
            var expectedEnumValue = enumConverter.Convert(LegalForm.Organization);
            var expectedEnumValueIndex = enumsDispatchObject.IndexOf(expectedEnumValue);
            var actualEnumValueIndex = enumsDispatchObject.IndexOf(valueTable[0]["ЮридическоеФизическоеЛицо"]);

            Assert.That(actualEnumValueIndex, Is.EqualTo(expectedEnumValueIndex));
        }

        [Test]
        public void CanAddCounterpartyWithNullableEnum()
        {
            var counterparty = new Контрагенты
            {
                ИНН = "1234567890",
                Наименование = "test-counterparty",
                ЮридическоеФизическоеЛицо = null
            };
            dataContext.Save(counterparty);
            Assert.That(string.IsNullOrEmpty(counterparty.Код), Is.False);

            var valueTable =
                globalContext.Execute("ВЫБРАТЬ * ИЗ Справочник.Контрагенты ГДЕ Код=&Code",
                    new Dictionary<string, object>
                    {
                        {"Code", counterparty.Код}
                    }).Unload();
            Assert.That(valueTable.Count, Is.EqualTo(1));
            Assert.That(valueTable[0].GetString("ИНН"), Is.EqualTo("1234567890"));
            Assert.That(valueTable[0].GetString("Наименование"), Is.EqualTo("test-counterparty"));
            Assert.That(ComHelpers.Invoke(valueTable[0]["ЮридическоеФизическоеЛицо"], "IsEmpty"), Is.True);
        }

        [Test]
        public void CanAddCounterpartyContract()
        {
            var organization = dataContext.Single<Организации>(x => x.ИНН == organizationInn);
            var counterparty = new Контрагенты
            {
                ИНН = "1234567890",
                Наименование = "test-counterparty",
                ЮридическоеФизическоеЛицо = ЮридическоеФизическоеЛицо.ЮридическоеЛицо
            };
            dataContext.Save(counterparty);

            var counterpartyFromStore = dataContext.Select<Контрагенты>().Single(x => x.Код == counterparty.Код);
            var counterpartyContract = new ДоговорыКонтрагентов
            {
                ВидДоговора = ВидыДоговоровКонтрагентов.СПокупателем,
                Наименование = "test name",
                Владелец = counterpartyFromStore,
                Организация = organization
            };
            dataContext.Save(counterpartyContract);
            Assert.That(string.IsNullOrEmpty(counterpartyContract.Код), Is.False);

            var valueTable =
                globalContext.Execute("ВЫБРАТЬ * ИЗ Справочник.ДоговорыКонтрагентов ГДЕ Код=&Code",
                    new Dictionary<string, object>
                    {
                        {"Code", counterpartyContract.Код}
                    }).Unload();
            Assert.That(valueTable.Count, Is.EqualTo(1));
            Assert.That(valueTable[0].GetString("Наименование"), Is.EqualTo("test name"));
            Assert.That(((dynamic) valueTable[0]["Владелец"]).Код, Is.EqualTo(counterparty.Код));
            valueTable =
                globalContext.Execute("ВЫБРАТЬ * ИЗ Справочник.Контрагенты ГДЕ Код=&Code",
                    new Dictionary<string, object>
                    {
                        {"Code", counterparty.Код}
                    }).Unload();
            Assert.That(valueTable.Count, Is.EqualTo(1));
        }

        [Test]
        public void ModifyCounterparty()
        {
            dynamic counterpartyAccessObject = testObjectsManager.CreateCounterparty(new Counterpart
            {
                Inn = "7711223344",
                Kpp = "771101001",
                FullName = "Test counterparty",
                Name = "Test counterparty"
            });
            string counterpartyCode = counterpartyAccessObject.Код;

            var counterparty = dataContext.Single<Контрагенты>(x => x.Код == counterpartyCode);
            counterparty.ИНН = "7711223344";
            counterparty.Наименование = "Test counterparty 2";
            counterparty.НаименованиеПолное = "Test counterparty 2";
            dataContext.Save(counterparty);
            var valueTable = globalContext.Execute("ВЫБРАТЬ * ИЗ Справочник.Контрагенты ГДЕ ИНН=&Inn",
                new Dictionary<string, object>
                {
                    {"Inn", "7711223344"}
                }).Unload();
            Assert.That(valueTable.Count, Is.EqualTo(1));
            Assert.That(valueTable[0].GetString("Наименование"), Is.EqualTo("Test counterparty 2"));
        }

        [Test]
        public void SelectMany()
        {
            var акт = new ПоступлениеТоваровУслуг
            {
                Дата = new DateTime(2016, 6, 1),
                Контрагент = new Контрагенты
                {
                    Наименование = "test contractor name",
                    ИНН = "test-inn"
                },
                Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>
                {
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура {Наименование = "чайник фарфоровый"},
                        Цена = 12,
                        Количество = 3
                    },
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура {Наименование = "самовар"},
                        Цена = 56,
                        Количество = 1
                    }
                }
            };
            dataContext.Save(акт);

            var акт2 = dataContext.Select<ПоступлениеТоваровУслуг>()
                .Where(x => x.Номер == акт.Номер && x.Дата == акт.Дата)
                .SelectMany(x => x.Услуги.Select(y => new
                {
                    x.Дата,
                    Контрагент_Инн = x.Контрагент.ИНН,
                    Контрагент_Наименование = x.Контрагент.Наименование,
                    Услуги_Номенклатура_Наименование = y.Номенклатура.Наименование,
                    Услуги_Номенклатура_Количество = y.Количество,
                    Услуги_Номенклатура_Цена = y.Цена
                }))
                .ToArray();
            Assert.That(акт2.Length, Is.EqualTo(2));

            Assert.That(акт2[0].Дата, Is.EqualTo(new DateTime(2016, 6, 1)));
            Assert.That(акт2[0].Контрагент_Инн, Is.EqualTo("test-inn"));
            Assert.That(акт2[0].Контрагент_Наименование, Is.EqualTo("test contractor name"));
            Assert.That(акт2[0].Услуги_Номенклатура_Наименование, Is.EqualTo("чайник фарфоровый"));
            Assert.That(акт2[0].Услуги_Номенклатура_Цена, Is.EqualTo(12));
            Assert.That(акт2[0].Услуги_Номенклатура_Количество, Is.EqualTo(3));

            Assert.That(акт2[1].Дата, Is.EqualTo(new DateTime(2016, 6, 1)));
            Assert.That(акт2[1].Контрагент_Инн, Is.EqualTo("test-inn"));
            Assert.That(акт2[1].Контрагент_Наименование, Is.EqualTo("test contractor name"));
            Assert.That(акт2[1].Услуги_Номенклатура_Наименование, Is.EqualTo("самовар"));
            Assert.That(акт2[1].Услуги_Номенклатура_Цена, Is.EqualTo(56));
            Assert.That(акт2[1].Услуги_Номенклатура_Количество, Is.EqualTo(1));
        }

        public interface IGenericCatalog
        {
            string Наименование { get; set; }
        }

        [Test]
        public void CanQueryWithSourceNameViaGenericInterface()
        {
            var counterpart = new Counterpart
            {
                Inn = "7711223344",
                Kpp = "771101001",
                FullName = "Test counterparty 1",
                Name = "Test counterparty 2"
            };
            testObjectsManager.CreateCounterparty(counterpart);

            var catalogItem = dataContext.Select<IGenericCatalog>("Справочник.Контрагенты")
                .Where(x => x.Наименование == "Test counterparty 2")
                .Cast<object>()
                .Single();
            Assert.That(catalogItem, Is.TypeOf<Контрагенты>());
        }

        [Test]
        public void CanQueryByReference()
        {
            var контрагент = new Контрагенты
            {
                Наименование = "test contractor name",
                ИНН = "test-inn"
            };
            var акт = new ПоступлениеТоваровУслуг
            {
                Дата = new DateTime(2016, 6, 1),
                Контрагент = контрагент,
                ДоговорКонтрагента = new ДоговорыКонтрагентов
                {
                    Наименование = "test contract",
                    Владелец = контрагент
                }
            };
            dataContext.Save(акт);
            var id = акт.Контрагент.УникальныйИдентификатор;
            var контрагент2 = dataContext.Single<Контрагенты>(x => x.УникальныйИдентификатор == id);

            var queryResult = dataContext.Select<ПоступлениеТоваровУслуг>()
                .Where(x => x.Контрагент == контрагент2)
                .ToArray();
            Assert.That(queryResult.Length, Is.EqualTo(1));
            Assert.That(queryResult[0].ДоговорКонтрагента.Наименование, Is.EqualTo("test contract"));
        }

        [Test]
        public void Count()
        {
            var контрагент = new Контрагенты
            {
                Наименование = "test contractor name",
                ИНН = "test-inn"
            };
            dataContext.Save(контрагент);
            var count = dataContext
                .Select<Контрагенты>()
                .Count(x => x.УникальныйИдентификатор == контрагент.УникальныйИдентификатор);
            Assert.That(count, Is.EqualTo(1));
        }
    }
}