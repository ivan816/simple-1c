using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Simple1C.Impl.Com;
using Simple1C.Interface;
using Simple1C.Tests.Metadata1C.Документы;
using Simple1C.Tests.Metadata1C.Перечисления;
using Simple1C.Tests.Metadata1C.ПланыСчетов;
using Simple1C.Tests.Metadata1C.РегистрыСведений;
using Simple1C.Tests.Metadata1C.Справочники;
using Simple1C.Tests.TestEntities;

namespace Simple1C.Tests
{
    public class COMDataContextTest : IntegrationTestBase
    {
        private IDataContext dataContext;
        private TestObjectsManager testObjectsManager;
        private EnumConverter enumConverter;

        protected override void SetUp()
        {
            base.SetUp();
            dataContext = DataContextFactory.CreateCOM(globalContext.ComObject(), typeof (Контрагенты).Assembly);
            enumConverter = new EnumConverter(globalContext);
            testObjectsManager = new TestObjectsManager(globalContext, enumConverter, organizationInn);
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

            var counterpartyContractAccessObject = testObjectsManager.CreateCounterpartContract(counterpartAccessObject.Ссылка, new CounterpartyContract
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
            Assert.That(((Контрагенты)contractFromStore.Владелец.ОсновнойБанковскийСчет.Владелец)
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
            Assert.That(((Контрагенты)account.Владелец).ИНН, Is.EqualTo("0987654321"));
            Assert.That(((Контрагенты)account.Владелец).КПП, Is.EqualTo("987654321"));
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
            var counterpartyContractAccessObject = testObjectsManager.CreateCounterpartContract(counterpartAccessObject.Ссылка,
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
        public void TableSections()
        {
            dynamic accessObject = testObjectsManager.CreateAccountingDocument(new AccountingDocument
            {
                Date = DateTime.Now,
                Number = "k-12345",
                Counterpart = new Counterpart
                {
                    Inn = "7711223344",
                    Name = "ООО РОмашка",
                    LegalForm = LegalForm.Organization
                },
                CounterpartContract = new CounterpartyContract
                {
                    CurrencyCode = "643",
                    Name = "Валюта",
                    Kind = CounterpartContractKind.OutgoingWithAgency
                },
                SumIncludesNds = true,
                IsCreatedByEmployee = false,
                Items = new[]
                {
                    new NomenclatureItem
                    {
                        Name = "Доставка воды",
                        NdsRate = NdsRate.NoNds,
                        Count = 1,
                        Price = 1000m,
                        Sum = 1000m,
                        NdsSum = 0m
                    }
                },
                Comment = "test comment",
                OperationKind = IncomingOperationKind.Services
            });

            string number = accessObject.Номер;
            DateTime date = accessObject.Дата;

            var acts = dataContext
                .Select<ПоступлениеТоваровУслуг>()
                .Single(x => x.Номер == number && x.Дата == date);
            Assert.That(acts.Услуги.Count, Is.EqualTo(1));
            Assert.That(acts.Услуги[0].Сумма, Is.EqualTo(1000m));
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

            var valueTable = globalContext.Execute("ВЫБРАТЬ * ИЗ Справочник.Контрагенты ГДЕ Код=&Code", new Dictionary<string, object>
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

            var valueTable = globalContext.Execute("ВЫБРАТЬ * ИЗ Справочник.ДоговорыКонтрагентов ГДЕ Код=&Code", new Dictionary<string, object>
            {
                {"Code", counterpartyContract.Код}
            }).Unload();
            Assert.That(valueTable.Count, Is.EqualTo(1));
            Assert.That(valueTable[0].GetString("Наименование"), Is.EqualTo("test name"));
            Assert.That(((dynamic)valueTable[0]["Владелец"]).Код, Is.EqualTo(counterparty.Код));
            valueTable = globalContext.Execute("ВЫБРАТЬ * ИЗ Справочник.Контрагенты ГДЕ Код=&Code", new Dictionary<string, object>
            {
                {"Code", counterparty.Код}
            }).Unload();
            Assert.That(valueTable.Count, Is.EqualTo(1));
        }

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
        public void CanWritePreviouslyReadInformationRegister()
        {
            var период = new DateTime(2025, 7, 17);
            var курс = new КурсыВалют
            {
                Валюта = dataContext.Single<Валюты>(x => x.Код == "643"),
                Кратность = 42,
                Курс = 12,
                Период = период
            };
            dataContext.Save(курс);

            var курс2 = dataContext.Single<КурсыВалют>(x => x.Период == период);
            курс2.Кратность = 43;
            dataContext.Save(курс2);

            var курс3 = dataContext.Single<КурсыВалют>(x => x.Период == период);
            Assert.That(курс3.Валюта.Код, Is.EqualTo("643"));
            Assert.That(курс3.Кратность, Is.EqualTo(43));
            Assert.That(курс3.Курс, Is.EqualTo(12));
            Assert.That(курс3.Период, Is.EqualTo(период));
        }

        [Test]
        public void CanReadWriteInformationRegister()
        {
            var период = new DateTime(2025, 7, 18);
            var курс = new КурсыВалют
            {
                Валюта = dataContext.Single<Валюты>(x => x.Код == "643"),
                Кратность = 42,
                Курс = 12,
                Период = период
            };
            dataContext.Save(курс);

            var курс2 = dataContext.Select<КурсыВалют>()
                .OrderByDescending(x => x.Период)
                .First();
            Assert.That(курс2.Валюта.Код, Is.EqualTo("643"));
            Assert.That(курс2.Кратность, Is.EqualTo(42));
            Assert.That(курс2.Курс, Is.EqualTo(12));
            Assert.That(курс2.Период, Is.EqualTo(период));

            курс.Кратность = 43;
            dataContext.Save(курс);

            var курс3 = dataContext.Select<КурсыВалют>()
                .OrderByDescending(x => x.Период)
                .First();
            Assert.That(курс3.Валюта.Код, Is.EqualTo("643"));
            Assert.That(курс3.Кратность, Is.EqualTo(43));
            Assert.That(курс3.Курс, Is.EqualTo(12));
            Assert.That(курс3.Период, Is.EqualTo(период));
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
        public void CanAddDocumentWithTableSection()
        {
            var поступлениеТоваровУслуг = new ПоступлениеТоваровУслуг
            {
                ДатаВходящегоДокумента = new DateTime(2016, 6, 1),
                Дата = new DateTime(2016, 6, 1),
                НомерВходящегоДокумента = "12345",
                ВидОперации = ВидыОперацийПоступлениеТоваровУслуг.Услуги,
                Контрагент = new Контрагенты
                {
                    ИНН = "7711223344",
                    Наименование = "ООО Тестовый контрагент",
                },
                Организация = ПолучитьТекущуюОрганизацию(),
                СпособЗачетаАвансов = СпособыЗачетаАвансов.Автоматически,
                Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>
                {
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура
                        {
                            Наименование = "стрижка"
                        },
                        Количество = 10,
                        Содержание = "стрижка с кудряшками",
                        Сумма = 120,
                        Цена = 12,
                        СтавкаНДС = СтавкиНДС.НДС18,
                        СуммаНДС = 21.6m
                    },
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура
                        {
                            Наименование = "мытье головы"
                        },
                        Количество = 10,
                        Содержание = "мытье головы хозяйственным мылом",
                        Сумма = 120,
                        Цена = 12,
                        СтавкаНДС = СтавкиНДС.НДС18,
                        СуммаНДС = 21.6m
                    }
                }
            };

            dataContext.Save(поступлениеТоваровУслуг);

            Assert.That(поступлениеТоваровУслуг.Номер, Is.Not.Null);
            Assert.That(поступлениеТоваровУслуг.Дата, Is.Not.EqualTo(default(DateTime)));
            Assert.That(поступлениеТоваровУслуг.Услуги.Count, Is.EqualTo(2));
            Assert.That(поступлениеТоваровУслуг.Услуги[0].НомерСтроки, Is.EqualTo(1));
            Assert.That(поступлениеТоваровУслуг.Услуги[1].НомерСтроки, Is.EqualTo(2));

            var valueTable = globalContext.Execute("Выбрать * ИЗ Документ.ПоступлениеТоваровУслуг ГДЕ Номер = &Number",
                new Dictionary<string, object>
                {
                    {"Number", поступлениеТоваровУслуг.Номер}
                }).Unload();

            Assert.That(valueTable.Count, Is.EqualTo(1));
            Assert.That(valueTable[0]["НомерВходящегоДокумента"], Is.EqualTo("12345"));
            Assert.That(valueTable[0]["ДатаВходящегоДокумента"], Is.EqualTo(new DateTime(2016, 6, 1)));

            var servicesTablePart = valueTable[0]["Услуги"] as dynamic;
            Assert.That(servicesTablePart.Count, Is.EqualTo(2));

            var row1 = servicesTablePart.Получить(0);
            Assert.That(row1.Номенклатура.Наименование, Is.EqualTo("стрижка"));
            Assert.That(row1.Количество, Is.EqualTo(10));
            Assert.That(row1.Содержание, Is.EqualTo("стрижка с кудряшками"));
            Assert.That(row1.Сумма, Is.EqualTo(120));
            Assert.That(row1.Цена, Is.EqualTo(12));

            var row2 = servicesTablePart.Получить(1);
            Assert.That(row2.Номенклатура.Наименование, Is.EqualTo("мытье головы"));
            Assert.That(row2.Содержание, Is.EqualTo("мытье головы хозяйственным мылом"));
        }

        [Test]
        public void CanModifyTableSection()
        {
            var поступлениеТоваровУслуг = new ПоступлениеТоваровУслуг
            {
                ДатаВходящегоДокумента = new DateTime(2016, 6, 1),
                Дата = new DateTime(2016, 6, 1),
                НомерВходящегоДокумента = "12345",
                ВидОперации = ВидыОперацийПоступлениеТоваровУслуг.Услуги,
                Контрагент = new Контрагенты
                {
                    ИНН = "7711223344",
                    Наименование = "ООО Тестовый контрагент",
                },
                Организация = ПолучитьТекущуюОрганизацию(),
                СпособЗачетаАвансов = СпособыЗачетаАвансов.Автоматически,
                Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>
                {
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура
                        {
                            Наименование = "стрижка"
                        },
                        Количество = 10,
                        Содержание = "стрижка с кудряшками",
                        Сумма = 120,
                        Цена = 12,
                        СтавкаНДС = СтавкиНДС.НДС18,
                        СуммаНДС = 21.6m
                    }
                }
            };
            dataContext.Save(поступлениеТоваровУслуг);

            поступлениеТоваровУслуг.Услуги[0].Содержание = "стрижка налысо";
            dataContext.Save(поступлениеТоваровУслуг);

            var valueTable = globalContext.Execute("Выбрать * ИЗ Документ.ПоступлениеТоваровУслуг ГДЕ Номер = &Number",
                new Dictionary<string, object>
                {
                    {"Number", поступлениеТоваровУслуг.Номер}
                }).Unload();

            Assert.That(valueTable.Count, Is.EqualTo(1));

            var servicesTablePart = valueTable[0]["Услуги"] as dynamic;
            Assert.That(servicesTablePart.Count, Is.EqualTo(1));

            var row1 = servicesTablePart.Получить(0);
            Assert.That(row1.Содержание, Is.EqualTo("стрижка налысо"));
        }

        [Test]
        public void CanDeleteItemFromTableSection()
        {
            var поступлениеТоваровУслуг = new ПоступлениеТоваровУслуг
            {
                ДатаВходящегоДокумента = new DateTime(2016, 6, 1),
                Дата = new DateTime(2016, 6, 1),
                НомерВходящегоДокумента = "12345",
                ВидОперации = ВидыОперацийПоступлениеТоваровУслуг.Услуги,
                Контрагент = new Контрагенты
                {
                    ИНН = "7711223344",
                    Наименование = "ООО Тестовый контрагент",
                },
                Организация =  ПолучитьТекущуюОрганизацию(),
                СпособЗачетаАвансов = СпособыЗачетаАвансов.Автоматически,
                Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>
                {
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура
                        {
                            Наименование = "стрижка"
                        },
                        Количество = 10,
                        Содержание = "стрижка с кудряшками",
                        Сумма = 120,
                        Цена = 12,
                        СтавкаНДС = СтавкиНДС.НДС18,
                        СуммаНДС = 21.6m
                    },
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура
                        {
                            Наименование = "стрижка усов"
                        },
                        Количество = 10,
                        Содержание = "стрижка бороды",
                        Сумма = 120,
                        Цена = 12,
                        СтавкаНДС = СтавкиНДС.НДС18,
                        СуммаНДС = 21.6m
                    },
                }
            };
            dataContext.Save(поступлениеТоваровУслуг);

            поступлениеТоваровУслуг.Услуги.RemoveAt(0);
            dataContext.Save(поступлениеТоваровУслуг);

            var valueTable = globalContext.Execute("Выбрать * ИЗ Документ.ПоступлениеТоваровУслуг ГДЕ Номер = &Number",
                new Dictionary<string, object>
                {
                    {"Number", поступлениеТоваровУслуг.Номер}
                }).Unload();

            Assert.That(valueTable.Count, Is.EqualTo(1));

            var servicesTablePart = valueTable[0]["Услуги"] as dynamic;
            Assert.That(servicesTablePart.Count, Is.EqualTo(1));

            var row1 = servicesTablePart.Получить(0);
            Assert.That(row1.Содержание, Is.EqualTo("стрижка бороды"));
        }

        [Test]
        public void CanChangeTableSectionItemsOrdering()
        {
            var поступлениеТоваровУслуг = new ПоступлениеТоваровУслуг
            {
                ДатаВходящегоДокумента = new DateTime(2016, 6, 1),
                Дата = new DateTime(2016, 6, 1),
                НомерВходящегоДокумента = "12345",
                ВидОперации = ВидыОперацийПоступлениеТоваровУслуг.Услуги,
                Контрагент = new Контрагенты
                {
                    ИНН = "7711223344",
                    Наименование = "ООО Тестовый контрагент",
                },
                Организация = ПолучитьТекущуюОрганизацию(),
                СпособЗачетаАвансов = СпособыЗачетаАвансов.Автоматически,
                Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>
                {
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура
                        {
                            Наименование = "стрижка"
                        },
                        Количество = 10,
                        Содержание = "стрижка с кудряшками",
                        Сумма = 120,
                        Цена = 12,
                        СтавкаНДС = СтавкиНДС.НДС18,
                        СуммаНДС = 21.6m
                    },
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура
                        {
                            Наименование = "стрижка усов"
                        },
                        Количество = 10,
                        Содержание = "стрижка бороды",
                        Сумма = 120,
                        Цена = 12,
                        СтавкаНДС = СтавкиНДС.НДС18,
                        СуммаНДС = 21.6m
                    },
                }
            };
            dataContext.Save(поступлениеТоваровУслуг);

            var t = поступлениеТоваровУслуг.Услуги[0];
            поступлениеТоваровУслуг.Услуги[0] = поступлениеТоваровУслуг.Услуги[1];
            поступлениеТоваровУслуг.Услуги[1] = t;
            dataContext.Save(поступлениеТоваровУслуг);

            var valueTable = globalContext.Execute("Выбрать * ИЗ Документ.ПоступлениеТоваровУслуг ГДЕ Номер = &Number",
                new Dictionary<string, object>
                {
                    {"Number", поступлениеТоваровУслуг.Номер}
                })
                .Unload();

            Assert.That(valueTable.Count, Is.EqualTo(1));

            var servicesTablePart = valueTable[0]["Услуги"] as dynamic;
            Assert.That(servicesTablePart.Count, Is.EqualTo(2));

            var row0 = servicesTablePart.Получить(0);
            Assert.That(row0.Содержание, Is.EqualTo("стрижка бороды"));

            var row1 = servicesTablePart.Получить(1);
            Assert.That(row1.Содержание, Is.EqualTo("стрижка с кудряшками"));
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

        public class TestDataContract
        {
            public DateTime Дата { get; set; }
            public string Контрагент_Инн { get; set; }
            public string Контрагент_Наименование { get; set; }
            public string ДоговорКонтрагента_Наименование { get; set; }
            public bool ДоговорКонтрагента_Валютный { get; set; }
            public string Грузополучатель_Наименование { get; set; }
            public string Грузополучатель_ИНН { get; set; }
        }

        [Test]
        public void ProjectionToRegularType()
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
                    Владелец = контрагент,
                    Наименование = "test contract",
                    Валютный = true
                }
            };
            dataContext.Save(акт);

            var акт2 = dataContext.Select<ПоступлениеТоваровУслуг>()
                .Where(x => x.Номер == акт.Номер && x.Дата == акт.Дата)
                .Select(x => new TestDataContract
                {
                    Дата = x.Дата.GetValueOrDefault(),
                    Контрагент_Инн = x.Контрагент.ИНН,
                    Контрагент_Наименование = x.Контрагент.Наименование,
                    ДоговорКонтрагента_Наименование = x.ДоговорКонтрагента.Наименование,
                    ДоговорКонтрагента_Валютный = x.ДоговорКонтрагента.Валютный,
                    Грузополучатель_Наименование = x.Грузополучатель.Наименование,
                    Грузополучатель_ИНН = x.Грузополучатель.ИНН
                })
                .ToArray();
            Assert.That(акт2.Length, Is.EqualTo(1));
            Assert.That(акт2[0].Дата, Is.EqualTo(new DateTime(2016, 6, 1)));
            Assert.That(акт2[0].Контрагент_Инн, Is.EqualTo("test-inn"));
            Assert.That(акт2[0].Контрагент_Наименование, Is.EqualTo("test contractor name"));
            Assert.That(акт2[0].ДоговорКонтрагента_Наименование, Is.EqualTo("test contract"));
            Assert.That(акт2[0].ДоговорКонтрагента_Валютный, Is.True);
            Assert.That(акт2[0].Грузополучатель_Наименование, Is.Null);
            Assert.That(акт2[0].Грузополучатель_ИНН, Is.Null);
        }

        [Test]
        public void ProjectionToAnonymousType()
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
                    Владелец = контрагент,
                    Наименование = "test contract",
                    Валютный = true
                }
            };
            dataContext.Save(акт);

            var акт2 = dataContext.Select<ПоступлениеТоваровУслуг>()
                .Where(x => x.Номер == акт.Номер && x.Дата == акт.Дата)
                .Select(x => new
                {
                    x.Дата, 
                    Контрагент_Инн = x.Контрагент.ИНН,
                    Контрагент_Наименование = x.Контрагент.Наименование,
                    ДоговорКонтрагента_Наименование = x.ДоговорКонтрагента.Наименование,
                    ДоговорКонтрагента_Валютный = x.ДоговорКонтрагента.Валютный,
                    Грузополучатель_Наименование = x.Грузополучатель.Наименование,
                    Грузополучатель_ИНН = x.Грузополучатель.ИНН
                })
                .ToArray();
            Assert.That(акт2.Length, Is.EqualTo(1));
            Assert.That(акт2[0].Дата, Is.EqualTo(new DateTime(2016, 6, 1)));
            Assert.That(акт2[0].Контрагент_Инн, Is.EqualTo("test-inn"));
            Assert.That(акт2[0].Контрагент_Наименование, Is.EqualTo("test contractor name"));
            Assert.That(акт2[0].ДоговорКонтрагента_Наименование, Is.EqualTo("test contract"));
            Assert.That(акт2[0].ДоговорКонтрагента_Валютный, Is.True);
            Assert.That(акт2[0].Грузополучатель_Наименование, Is.Null);
            Assert.That(акт2[0].Грузополучатель_ИНН, Is.Null);
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

        [Test]
        public void DoNotOverwriteDocumentWhenObservedListDoesNotChange()
        {
            var акт = new ПоступлениеТоваровУслуг
            {
                Дата = new DateTime(2016, 6, 1),
                Контрагент = new Контрагенты
                {
                    Наименование = "contractor name"
                }
            };
            dataContext.Save(акт);

            var docVersion = GetDocumentByNumber(акт.Номер).DataVersion;

            var акт2 = dataContext.Single<ПоступлениеТоваровУслуг>(x => x.Номер == акт.Номер && x.Дата == акт.Дата);
            Assert.That(акт2.Услуги.Count, Is.EqualTo(0));
            акт2.Контрагент.Наименование = "changed contractor name";
            dataContext.Save(акт2);

            var контрагент = dataContext.Single<Контрагенты>(x => x.Код == акт.Контрагент.Код);
            Assert.That(контрагент.Наименование, Is.EqualTo("changed contractor name"));
            var newDocVersion = GetDocumentByNumber(акт.Номер).DataVersion;
            Assert.That(newDocVersion, Is.EqualTo(docVersion));
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
        public void RecursiveSave()
        {
            var контрагентВася = new Контрагенты
            {
                Наименование = "Василий"
            };
            контрагентВася.ГоловнойКонтрагент = контрагентВася;
            var exception = Assert.Throws<InvalidOperationException>(()=> dataContext.Save(контрагентВася));
            Assert.That(exception.Message, Does.Contain("cycle detected for entity type [Контрагенты]: [Контрагенты->ГоловнойКонтрагент]"));

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

        private ПоступлениеТоваровУслуг CreateFullFilledDocument()
        {
            var контрагент = new Контрагенты
            {
                ИНН = "7711223344",
                Наименование = "ООО Тестовый контрагент"
            };
            var организация = dataContext.Single<Организации>(x => x.ИНН == organizationInn);
            var валютаВзаиморасчетов = dataContext.Single<Валюты>(x => x.Код == "643");
            var договорСКонтрагентом = new ДоговорыКонтрагентов
            {
                Владелец = контрагент,
                Организация = организация,
                ВидДоговора = ВидыДоговоровКонтрагентов.СПоставщиком,
                Наименование = "test contract",
                Комментарий = "test contract comment",
                ВалютаВзаиморасчетов = валютаВзаиморасчетов
            };
            var счет26 = dataContext.Single<Хозрасчетный>(x => x.Код == "26");
            var счет1904 = dataContext.Single<Хозрасчетный>(x => x.Код == "19.04");
            var счет6001 = dataContext.Single<Хозрасчетный>(x => x.Код == "60.01");
            var счет6002 = dataContext.Single<Хозрасчетный>(x => x.Код == "60.02");
            var материальныеРасходы = new СтатьиЗатрат
            {
                Наименование = "Материальные расходы",
                ВидРасходовНУ = ВидыРасходовНУ.МатериальныеРасходы
            };
            return new ПоступлениеТоваровУслуг
            {
                ДатаВходящегоДокумента = new DateTime(2016, 6, 1),
                Дата = new DateTime(2016, 6, 1),
                НомерВходящегоДокумента = "12345",
                ВидОперации = ВидыОперацийПоступлениеТоваровУслуг.Услуги,
                Контрагент = контрагент,
                ДоговорКонтрагента = договорСКонтрагентом,
                Организация = организация,
                СпособЗачетаАвансов = СпособыЗачетаАвансов.Автоматически,
                ВалютаДокумента = валютаВзаиморасчетов,
                СчетУчетаРасчетовСКонтрагентом = счет6001,
                СчетУчетаРасчетовПоАвансам = счет6002,
                Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>
                {
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура
                        {
                            Наименование = "стрижка"
                        },
                        Количество = 10,
                        Содержание = "стрижка с кудряшками",
                        Сумма = 120,
                        Цена = 12,
                        СтавкаНДС = СтавкиНДС.НДС18,
                        СуммаНДС = 21.6m,
                        СчетЗатрат = счет26,
                        СчетЗатратНУ = счет26,
                        СчетУчетаНДС = счет1904,
                        ОтражениеВУСН = ОтражениеВУСН.Принимаются,
                        Субконто1 = материальныеРасходы,
                        ПодразделениеЗатрат = ПолучитьОсновноеПодразделение()
                    },
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура
                        {
                            Наименование = "мытье головы"
                        },
                        Количество = 10,
                        Содержание = "мытье головы хозяйственным мылом",
                        Сумма = 120,
                        Цена = 12,
                        СтавкаНДС = СтавкиНДС.НДС18,
                        СуммаНДС = 21.6m,
                        СчетЗатрат = счет26,
                        СчетЗатратНУ = счет26,
                        СчетУчетаНДС = счет1904,
                        ОтражениеВУСН = ОтражениеВУСН.Принимаются,
                        Субконто1 = материальныеРасходы,
                        ПодразделениеЗатрат = ПолучитьОсновноеПодразделение()
                    }
                }
            };
        }

        private ПодразделенияОрганизаций ПолучитьОсновноеПодразделение()
        {
            var организация = ПолучитьТекущуюОрганизацию();
            return dataContext.Single<ПодразделенияОрганизаций>(
                x => x.Владелец.Код == организация.Код,
                x => !x.ПометкаУдаления,
                x => x.Код == "00-000001"
                     || x.Код == "000000001"
                     || x.Код == "99-000001"
                     || x.Наименование == "Основное подразделение");
        }

        private Организации ПолучитьТекущуюОрганизацию()
        {
            return dataContext.Single<Организации>(
                x => !x.ПометкаУдаления,
                x => x.ИНН == organizationInn);
        }

        private dynamic GetDocumentByNumber(string number)
        {
            var valueTable = globalContext.Execute("Выбрать * ИЗ Документ.ПоступлениеТоваровУслуг ГДЕ Номер = &Number",
                new Dictionary<string, object>
                {
                    {"Number", number}
                }).Unload();
            Assert.That(valueTable.Count, Is.EqualTo(1));
            return valueTable[0]["Ссылка"];
        }

        private object CreateTestCounterpart()
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
            return counterpartAccessObject;
        }
    }
}