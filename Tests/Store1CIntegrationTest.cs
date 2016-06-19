using System.Linq;
using NUnit.Framework;
using Simple1C.Tests.Metadata1C.Справочники;

namespace Simple1C.Tests
{
    //[Inject] public CounterpartManager counterpartManager;
    //[Inject] public CounterpartContractManager counterpartContractManager;
    //[Inject] public BankAccountManager bankAccountManager;
    //[Inject] public ICatalogRegistry catalogRegistry;
    //[Inject] public IIncomingAccountingDocumentManager incomingAccountingDocumentManager;
    //[Inject] public IQueryExecuter queryExecuter;
    //[Inject] public EnumerationManager enumerationManager;
    //[Inject] public ХранилищеПодразделений хранилищеПодразделений;
    public class Store1CIntegrationTest : IntegrationTestBase
    {
        private DataContext dataContext;

        protected override void SetUp()
        {
            base.SetUp();
            dataContext = new DataContext(globalContext);
        }

        [Test]
        public void Simple()
        {
            //counterpartManager.Create(new Counterpart
            //{
            //    Name = "test-name",
            //    Inn = "1234567890",
            //    Kpp = "123456789"
            //});

            var instance = dataContext
                .Select<Контрагенты>()
                .Single(x => x.ИНН == "1234567890");
            Assert.That(instance.Наименование, Is.EqualTo("test-name"));
            Assert.That(instance.ИНН, Is.EqualTo("1234567890"));
            Assert.That(instance.КПП, Is.EqualTo("123456789"));
        }

        //[Test]
        //public void SelectWithRef()
        //{
        //    var counterpart = new Counterpart
        //    {
        //        Name = "test-counterpart-name",
        //        Inn = "0987654321",
        //        Kpp = "987654321"
        //    };
        //    var counterpartAccessObject = counterpartManager.Create(counterpart);
        //    counterpart.Code = counterpartAccessObject.Code;
        //    var bankAccountAccessObject = bankAccountManager.CreateAccount(counterpartAccessObject.Code, BankAccountOwnerType.JuridicalCounterparty,
        //        new BankAccount
        //        {
        //            Bank = new Bank
        //            {
        //                Bik = Banks.AlfaBankBik
        //            },
        //            Number = "40702810001111122222",
        //            CurrencyCode = "643"
        //        });

        //    counterpartAccessObject.DefaultBankAccount = bankAccountAccessObject.Reference;
        //    counterpartAccessObject.Write();

        //    var counterpartyContractCode = counterpartContractManager.Create(counterpart, new CounterpartyContract
        //    {
        //        CounterpartyCode = counterpartAccessObject.Code,
        //        CurrencyCode = "643",
        //        Name = "Валюта",
        //        Type = CounterpartContractKind.OutgoingWithAgency
        //    }).Code;

        //    var contractFromStore = store1C
        //        .Select<ДоговорыКонтрагентов>()
        //        .Single(x => x.Код == counterpartyContractCode);

        //    Assert.That(contractFromStore.Владелец.ИНН, Is.EqualTo("0987654321"));
        //    Assert.That(contractFromStore.Владелец.КПП, Is.EqualTo("987654321"));
        //    Assert.That(contractFromStore.Владелец.Наименование, Is.EqualTo("test-counterpart-name"));
        //    Assert.That(contractFromStore.Владелец.ОсновнойБанковскийСчет.НомерСчета,
        //                Is.EqualTo("40702810001111122222"));
        //    Assert.That(contractFromStore.Владелец.ОсновнойБанковскийСчет.Владелец,
        //                Is.TypeOf<Контрагенты>());
        //    Assert.That(((Контрагенты)contractFromStore.Владелец.ОсновнойБанковскийСчет.Владелец)
        //            .ИНН,
        //        Is.EqualTo("0987654321"));
        //    Assert.That(contractFromStore.ВидДоговора, Is.EqualTo(ВидыДоговоровКонтрагентов.СКомиссионеромНаЗакупку));
        //}

        //[Test]
        //public void QueryWithRefAccess()
        //{
        //    var counterpart = new Counterpart
        //    {
        //        Name = "test-counterpart-name",
        //        Inn = "0987654321",
        //        Kpp = "987654321"
        //    };
        //    var counterpartAccessObject = counterpartManager.Create(counterpart);
        //    counterpart.Code = counterpartAccessObject.Code;
        //    var bankAccountAccessObject = bankAccountManager.CreateAccount(counterpartAccessObject.Code, BankAccountOwnerType.JuridicalCounterparty,
        //        new BankAccount
        //        {
        //            Bank = new Bank
        //            {
        //                Bik = Banks.AlfaBankBik
        //            },
        //            Number = "40702810001111122222",
        //            CurrencyCode = "643"
        //        });

        //    counterpartAccessObject.DefaultBankAccount = bankAccountAccessObject.Reference;
        //    counterpartAccessObject.Write();

        //    counterpartContractManager.Create(counterpart, new CounterpartyContract
        //    {
        //        CounterpartyCode = counterpartAccessObject.Code,
        //        CurrencyCode = "643",
        //        Name = "Валюта",
        //        Type = CounterpartContractKind.OutgoingWithAgency
        //    });

        //    var contractFromStore = store1C.Select<ДоговорыКонтрагентов>()
        //        .Single(x => x.Владелец.ОсновнойБанковскийСчет.НомерСчета == "40702810001111122222");
        //    Assert.That(contractFromStore.Наименование, Is.EqualTo("Валюта"));
        //}

        //[Test]
        //public void QueryWithObject()
        //{
        //    var counterpart = new Counterpart
        //    {
        //        Name = "test-counterpart-name",
        //        Inn = "0987654321",
        //        Kpp = "987654321"
        //    };
        //    var counterpartAccessObject = counterpartManager.Create(counterpart);
        //    counterpart.Code = counterpartAccessObject.Code;
        //    bankAccountManager.CreateAccount(counterpartAccessObject.Code, BankAccountOwnerType.JuridicalCounterparty,
        //        new BankAccount
        //        {
        //            Bank = new Bank
        //            {
        //                Bik = Banks.AlfaBankBik
        //            },
        //            Number = "40702810001111122222",
        //            CurrencyCode = "643"
        //        });

        //    var account = store1C.Select<БанковскиеСчета>()
        //        .Single(x => x.Владелец is Контрагенты);
        //    Assert.That(account.ВалютаДенежныхСредств.Код, Is.EqualTo("643"));
        //    Assert.That(account.Владелец, Is.TypeOf<Контрагенты>());
        //    Assert.That(((Контрагенты)account.Владелец).ИНН, Is.EqualTo("0987654321"));
        //    Assert.That(((Контрагенты)account.Владелец).КПП, Is.EqualTo("987654321"));
        //}

        //[Test]
        //public void NullableEnumCanSet()
        //{
        //    var counterpart = new Counterpart
        //    {
        //        Name = "test-counterpart-name",
        //        Inn = "0987654321",
        //        Kpp = "987654321"
        //    };
        //    var counterpartAccessObject = counterpartManager.Create(counterpart);
        //    counterpart.Code = counterpartAccessObject.Code;
        //    bankAccountManager.CreateAccount(counterpartAccessObject.Code, BankAccountOwnerType.JuridicalCounterparty,
        //        new BankAccount
        //        {
        //            Bank = new Bank
        //            {
        //                Bik = Banks.AlfaBankBik
        //            },
        //            Number = "40702810001111122222",
        //            CurrencyCode = "643"
        //        });

        //    counterpartContractManager.Create(counterpart, new CounterpartyContract
        //    {
        //        CounterpartyCode = counterpartAccessObject.Code,
        //        CurrencyCode = "643",
        //        Name = "Валюта",
        //        Type = CounterpartContractKind.OutgoingWithAgency
        //    });

        //    var contracts = store1C.Select<ДоговорыКонтрагентов>()
        //        .Where(x => x.Владелец.Код == counterpart.Code)
        //        .ToArray();

        //    Assert.That(contracts.Length, Is.EqualTo(1));
        //    Assert.That(contracts[0].ВидДоговора, Is.EqualTo(ВидыДоговоровКонтрагентов.СКомиссионеромНаЗакупку));
        //}

        //[Test]
        //public void EnumParameterMapping()
        //{
        //    var counterpart = new Counterpart
        //    {
        //        Name = "test-counterpart-name",
        //        Inn = "0987654321",
        //        Kpp = "987654321"
        //    };
        //    var counterpartAccessObject = counterpartManager.Create(counterpart);
        //    counterpart.Code = counterpartAccessObject.Code;
        //    bankAccountManager.CreateAccount(counterpartAccessObject.Code, BankAccountOwnerType.JuridicalCounterparty,
        //        new BankAccount
        //        {
        //            Bank = new Bank
        //            {
        //                Bik = Banks.AlfaBankBik
        //            },
        //            Number = "40702810001111122222",
        //            CurrencyCode = "643"
        //        });

        //    counterpartContractManager.Create(counterpart, new CounterpartyContract
        //    {
        //        CounterpartyCode = counterpartAccessObject.Code,
        //        CurrencyCode = "643",
        //        Name = "Валюта",
        //        Type = CounterpartContractKind.OutgoingWithAgency
        //    });

        //    var contracts = store1C.Select<ДоговорыКонтрагентов>()
        //        .Where(x => x.ВидДоговора == ВидыДоговоровКонтрагентов.СКомиссионеромНаЗакупку)
        //        .ToArray();

        //    Assert.That(contracts.Length, Is.EqualTo(1));
        //    Assert.That(contracts[0].ВидДоговора, Is.EqualTo(ВидыДоговоровКонтрагентов.СКомиссионеромНаЗакупку));
        //}

        //[Test]
        //public void CanFilterForEmptyReference()
        //{
        //    var counterpartAccessObject = CreateTestCounterpart();
        //    var counterpartContractAccessObject = catalogRegistry.GetManager<CounterpertContractCatalog>().CreateItem();
        //    counterpartContractAccessObject.Owner = counterpartAccessObject.Reference;
        //    counterpartContractAccessObject.Description = "test-description";
        //    counterpartContractAccessObject.Write();

        //    var contracts = store1C.Select<ДоговорыКонтрагентов>()
        //        .Where(x => x.Код == counterpartContractAccessObject.Code)
        //        .Where(x => x.ТипЦен == null)
        //        .ToArray();
        //    Assert.That(contracts.Length, Is.EqualTo(1));
        //    Assert.That(contracts[0].Код, Is.EqualTo(counterpartContractAccessObject.Code));
        //    Assert.That(contracts[0].ТипЦен, Is.Null);

        //    contracts = store1C.Select<ДоговорыКонтрагентов>()
        //        .Where(x => x.Код == counterpartContractAccessObject.Code)
        //        .Where(x => x.ТипЦен != null)
        //        .ToArray();
        //    Assert.That(contracts.Length, Is.EqualTo(0));

        //    contracts = store1C.Select<ДоговорыКонтрагентов>()
        //        .Where(x => counterpartContractAccessObject.Code == x.Код)
        //        .Where(x => null == x.ТипЦен)
        //        .ToArray();
        //    Assert.That(contracts.Length, Is.EqualTo(1));
        //    Assert.That(contracts[0].Код, Is.EqualTo(counterpartContractAccessObject.Code));
        //    Assert.That(contracts[0].ТипЦен, Is.Null);
        //}

        //[Test]
        //public void TableSections()
        //{
        //    var accessObject = incomingAccountingDocumentManager.Create(new AccountingDocument
        //    {
        //        Date = DateTime.Now,
        //        Number = "k-12345",
        //        Counterpart = new Counterpart
        //        {
        //            Inn = "7711223344",
        //            Name = "ООО РОмашка",
        //            LegalForm = LegalForm.Organization
        //        },
        //        Kind = DocumentKind.Incoming,
        //        NeedPosting = false,
        //        SumIncludesNds = true,
        //        IsCreatedByEmployee = false,
        //        NdsRate = NdsRate.NoNds,
        //        Nomenclatures = new[]
        //        {
        //            new Nomenclature
        //            {
        //                Name = "Доставка воды",
        //                NdsRate = NdsRate.NoNds,
        //                Type = NomenclatureType.Service,
        //                Count = 1,
        //                Price = 1000m,
        //                Sum = 1000m,
        //                ServiceDescription = "Доставка воды",
        //                Units = "шт.",
        //                NdsSum = 0m,
        //            }
        //        }
        //    });

        //    var acts = store1C
        //        .Select<ПоступлениеТоваровУслуг>()
        //        .Single(x => x.Номер == accessObject.Number && x.Дата == accessObject.Date);
        //    Assert.That(acts.Услуги.Count, Is.EqualTo(1));
        //    Assert.That(acts.Услуги[0].Сумма, Is.EqualTo(1000m));
        //}

        //[Test]
        //public void CanAddCounterparty()
        //{
        //    var counterparty = new Контрагенты
        //    {
        //        ИНН = "1234567890",
        //        Наименование = "test-counterparty",
        //        ЮридическоеФизическоеЛицо = ЮридическоеФизическоеЛицо.ЮридическоеЛицо
        //    };
        //    store1C.Save(counterparty);
        //    Assert.That(string.IsNullOrEmpty(counterparty.Код), Is.False);

        //    var valueTable = queryExecuter.Execute("ВЫБРАТЬ * ИЗ Справочник.Контрагенты ГДЕ Код=&Code", new Dictionary<string, object>
        //    {
        //        {"Code", counterparty.Код}
        //    });
        //    Assert.That(valueTable.Count, Is.EqualTo(1));
        //    Assert.That(valueTable[0].GetString("ИНН"), Is.EqualTo("1234567890"));
        //    Assert.That(valueTable[0].GetString("Наименование"), Is.EqualTo("test-counterparty"));
        //    Assert.That(
        //        enumerationManager.Is(
        //            valueTable[0].GetDispatchObject<Enumeration<LegalForm>>("ЮридическоеФизическоеЛицо"),
        //            LegalForm.Organization));
        //}

        //[Test]
        //public void CanAddCounterpartyWithNullableEnum()
        //{
        //    var counterparty = new Контрагенты
        //    {
        //        ИНН = "1234567890",
        //        Наименование = "test-counterparty",
        //        ЮридическоеФизическоеЛицо = null
        //    };
        //    store1C.Save(counterparty);
        //    Assert.That(string.IsNullOrEmpty(counterparty.Код), Is.False);

        //    var valueTable = queryExecuter.Execute("ВЫБРАТЬ * ИЗ Справочник.Контрагенты ГДЕ Код=&Code", new Dictionary<string, object>
        //    {
        //        {"Code", counterparty.Код}
        //    });
        //    Assert.That(valueTable.Count, Is.EqualTo(1));
        //    Assert.That(valueTable[0].GetString("ИНН"), Is.EqualTo("1234567890"));
        //    Assert.That(valueTable[0].GetString("Наименование"), Is.EqualTo("test-counterparty"));
        //    Assert.That(ComHelpers.Invoke(valueTable[0]["ЮридическоеФизическоеЛицо"], "IsEmpty"), Is.True);
        //}

        //[Test]
        //public void CanAddCounterpartyContract()
        //{
        //    var organization = store1C.Select<Организации>().Single(x => x.ИНН == organizationInn);
        //    var counterparty = new Контрагенты
        //    {
        //        ИНН = "1234567890",
        //        Наименование = "test-counterparty",
        //        ЮридическоеФизическоеЛицо = ЮридическоеФизическоеЛицо.ЮридическоеЛицо
        //    };
        //    store1C.Save(counterparty);

        //    var counterpartyFromStore = store1C.Select<Контрагенты>().Single(x => x.Код == counterparty.Код);
        //    var counterpartyContract = new ДоговорыКонтрагентов
        //    {
        //        ВидДоговора = ВидыДоговоровКонтрагентов.СПокупателем,
        //        Наименование = "test name",
        //        Владелец = counterpartyFromStore,
        //        Организация = organization
        //    };
        //    store1C.Save(counterpartyContract);
        //    Assert.That(string.IsNullOrEmpty(counterpartyContract.Код), Is.False);

        //    var valueTable = queryExecuter.Execute("ВЫБРАТЬ * ИЗ Справочник.ДоговорыКонтрагентов ГДЕ Код=&Code", new Dictionary<string, object>
        //    {
        //        {"Code", counterpartyContract.Код}
        //    });
        //    Assert.That(valueTable.Count, Is.EqualTo(1));
        //    Assert.That(valueTable[0].GetString("Наименование"), Is.EqualTo("test name"));
        //    Assert.That(valueTable[0].GetDispatchObject<Reference<CounterpartAccessObject>>("Владелец").Object.Code,
        //        Is.EqualTo(counterparty.Код));
        //    valueTable = queryExecuter.Execute("ВЫБРАТЬ * ИЗ Справочник.Контрагенты ГДЕ Код=&Code", new Dictionary<string, object>
        //    {
        //        {"Code", counterparty.Код}
        //    });
        //    Assert.That(valueTable.Count, Is.EqualTo(1));
        //}

        //[Test]
        //public void CanAddRecursive()
        //{
        //    var organization = store1C.Select<Организации>().Single(x => x.ИНН == organizationInn);
        //    var counterpartyContract = new ДоговорыКонтрагентов
        //    {
        //        ВидДоговора = ВидыДоговоровКонтрагентов.СПокупателем,
        //        Наименование = "test name",
        //        Владелец = new Контрагенты
        //        {
        //            ИНН = "1234567890",
        //            Наименование = "test-counterparty",
        //            ЮридическоеФизическоеЛицо = ЮридическоеФизическоеЛицо.ЮридическоеЛицо
        //        },
        //        Организация = organization
        //    };
        //    store1C.Save(counterpartyContract);
        //    Assert.That(string.IsNullOrEmpty(counterpartyContract.Код), Is.False);
        //    Assert.That(string.IsNullOrEmpty(counterpartyContract.Владелец.Код), Is.False);

        //    var valueTable = queryExecuter.Execute("ВЫБРАТЬ * ИЗ Справочник.ДоговорыКонтрагентов ГДЕ Код=&Code", new Dictionary<string, object>
        //    {
        //        {"Code", counterpartyContract.Код}
        //    });
        //    Assert.That(valueTable.Count, Is.EqualTo(1));
        //    Assert.That(valueTable[0].GetString("Наименование"), Is.EqualTo("test name"));
        //}

        //[Test]
        //public void ChangeMustBeStrongerThanTracking()
        //{
        //    var counterpart = new Counterpart
        //    {
        //        Inn = "7711223344",
        //        Kpp = "771101001",
        //        FullName = "Test counterparty",
        //        Name = "Test counterparty"
        //    };
        //    var counterpartyAccessObject = counterpartManager.Create(counterpart);
        //    counterpart.Code = counterpartyAccessObject.Code;

        //    var counterpartContractAccessObject = counterpartContractManager.Create(counterpart,
        //        CounterpartContractKind.Incoming, "643");

        //    var contract = store1C.Select<ДоговорыКонтрагентов>()
        //        .Single(x => x.Код == counterpartContractAccessObject.Code);
        //    if (contract.Владелец.ИНН == "7711223344")
        //    {
        //        contract.Владелец.ИНН = "7711223345";
        //        contract.Владелец = new Контрагенты
        //        {
        //            ИНН = "7711223355",
        //            Наименование = "Test counterparty 2",
        //            НаименованиеПолное = "Test counterparty 2"
        //        };
        //    }
        //    store1C.Save(contract);

        //    var valueTable = queryExecuter.Execute("ВЫБРАТЬ * ИЗ Справочник.ДоговорыКонтрагентов ГДЕ Код=&Code",
        //        new Dictionary<string, object>
        //        {
        //            {"Code", contract.Код}
        //        });
        //    Assert.That(valueTable.Count, Is.EqualTo(1));
        //    Assert.That(ComHelpers.GetProperty(valueTable[0]["Владелец"], "Наименование"), Is.EqualTo("Test counterparty 2"));
        //}

        //[Test]
        //public void ModifyCounterparty()
        //{
        //    var counterpartyAccessObject = counterpartManager.Create(new Counterpart
        //    {
        //        Inn = "7711223344",
        //        Kpp = "771101001",
        //        FullName = "Test counterparty",
        //        Name = "Test counterparty"
        //    });

        //    var counterparty = store1C.Select<Контрагенты>().Single(x => x.Код == counterpartyAccessObject.Code);
        //    counterparty.ИНН = "7711223344";
        //    counterparty.Наименование = "Test counterparty 2";
        //    counterparty.НаименованиеПолное = "Test counterparty 2";
        //    store1C.Save(counterparty);
        //    var valueTable = queryExecuter.Execute("ВЫБРАТЬ * ИЗ Справочник.Контрагенты ГДЕ ИНН=&Inn",
        //        new Dictionary<string, object>
        //        {
        //            {"Inn", "7711223344"}
        //        });
        //    Assert.That(valueTable.Count, Is.EqualTo(1));
        //    Assert.That(valueTable[0].GetString("Наименование"), Is.EqualTo("Test counterparty 2"));
        //}

        //[Test]
        //public void CanUnpostDocument()
        //{
        //    var поступлениеТоваровУслуг = CreateFullFilledDocument();
        //    поступлениеТоваровУслуг.Проведен = true;
        //    store1C.Save(поступлениеТоваровУслуг);

        //    поступлениеТоваровУслуг.Услуги[0].Содержание = "каре";
        //    store1C.Save(поступлениеТоваровУслуг);

        //    var document = GetDocumentByNumber(поступлениеТоваровУслуг.Номер);
        //    Assert.That(document.Проведен, Is.True);
        //    Assert.That(document.Услуги.Получить(0).Содержание, Is.EqualTo("каре"));
        //}

        //[Test]
        //public void CanPostDocuments()
        //{
        //    var поступлениеТоваровУслуг = CreateFullFilledDocument();
        //    store1C.Save(поступлениеТоваровУслуг);
        //    Assert.That(GetDocumentByNumber(поступлениеТоваровУслуг.Номер).Проведен, Is.False);
        //    поступлениеТоваровУслуг.Проведен = true;
        //    store1C.Save(поступлениеТоваровУслуг);
        //    Assert.That(GetDocumentByNumber(поступлениеТоваровУслуг.Номер).Проведен);
        //}

        //private ПоступлениеТоваровУслуг CreateFullFilledDocument()
        //{
        //    var контрагент = new Контрагенты
        //    {
        //        ИНН = "7711223344",
        //        Наименование = "ООО Тестовый контрагент"
        //    };
        //    var организация = store1C.Single<Организации>(x => x.ИНН == organizationInn);
        //    var валютаВзаиморасчетов = store1C.Single<Валюты>(x => x.Код == "643");
        //    var договорСКонтрагентом = new ДоговорыКонтрагентов
        //    {
        //        Владелец = контрагент,
        //        Организация = организация,
        //        ВидДоговора = ВидыДоговоровКонтрагентов.СПоставщиком,
        //        Наименование = "test contract",
        //        Комментарий = "test contract comment",
        //        ВалютаВзаиморасчетов = валютаВзаиморасчетов
        //    };
        //    var счет26 = store1C.Single<Хозрасчетный>(x => x.Код == "26");
        //    var счет1904 = store1C.Single<Хозрасчетный>(x => x.Код == "19.04");
        //    var счет6001 = store1C.Single<Хозрасчетный>(x => x.Код == "60.01");
        //    var счет6002 = store1C.Single<Хозрасчетный>(x => x.Код == "60.02");
        //    var материальныеРасходы = new СтатьиЗатрат
        //    {
        //        Наименование = "Материальные расходы",
        //        ВидРасходовНУ = ВидыРасходовНУ.МатериальныеРасходы
        //    };
        //    return new ПоступлениеТоваровУслуг
        //    {
        //        ДатаВходящегоДокумента = new DateTime(2016, 6, 1),
        //        Дата = new DateTime(2016, 6, 1),
        //        НомерВходящегоДокумента = "12345",
        //        ВидОперации = ВидыОперацийПоступлениеТоваровУслуг.Услуги,
        //        Контрагент = контрагент,
        //        ДоговорКонтрагента = договорСКонтрагентом,
        //        Организация = организация,
        //        СпособЗачетаАвансов = СпособыЗачетаАвансов.Автоматически,
        //        ВалютаДокумента = валютаВзаиморасчетов,
        //        СчетУчетаРасчетовСКонтрагентом = счет6001,
        //        СчетУчетаРасчетовПоАвансам = счет6002,
        //        Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>
        //        {
        //            new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
        //            {
        //                Номенклатура = new Номенклатура
        //                {
        //                    Наименование = "стрижка"
        //                },
        //                Количество = 10,
        //                Содержание = "стрижка с кудряшками",
        //                Сумма = 120,
        //                Цена = 12,
        //                СтавкаНДС = СтавкиНДС.НДС18,
        //                СуммаНДС = 21.6m,
        //                СчетЗатрат = счет26,
        //                СчетЗатратНУ = счет26,
        //                СчетУчетаНДС = счет1904,
        //                ОтражениеВУСН = ОтражениеВУСН.Принимаются,
        //                Субконто1 = материальныеРасходы,
        //                ПодразделениеЗатрат = хранилищеПодразделений.ПолучитьОсновноеПодразделение()
        //            },
        //            new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
        //            {
        //                Номенклатура = new Номенклатура
        //                {
        //                    Наименование = "мытье головы"
        //                },
        //                Количество = 10,
        //                Содержание = "мытье головы хозяйственным мылом",
        //                Сумма = 120,
        //                Цена = 12,
        //                СтавкаНДС = СтавкиНДС.НДС18,
        //                СуммаНДС = 21.6m,
        //                СчетЗатрат = счет26,
        //                СчетЗатратНУ = счет26,
        //                СчетУчетаНДС = счет1904,
        //                ОтражениеВУСН = ОтражениеВУСН.Принимаются,
        //                Субконто1 = материальныеРасходы,
        //                ПодразделениеЗатрат = хранилищеПодразделений.ПолучитьОсновноеПодразделение()
        //            }
        //        }
        //    };
        //}

        //private dynamic GetDocumentByNumber(string number)
        //{
        //    var valueTable = queryExecuter.Execute("Выбрать * ИЗ Документ.ПоступлениеТоваровУслуг ГДЕ Номер = &Number",
        //        new Dictionary<string, object>
        //        {
        //            {"Number", number}
        //        });
        //    Assert.That(valueTable.Count, Is.EqualTo(1));
        //    return valueTable[0]["Ссылка"];
        //}

        //[Test]
        //public void CanAddDocumentWithTableSection()
        //{
        //    var поступлениеТоваровУслуг = new ПоступлениеТоваровУслуг
        //    {
        //        ДатаВходящегоДокумента = new DateTime(2016, 6, 1),
        //        Дата = new DateTime(2016, 6, 1),
        //        НомерВходящегоДокумента = "12345",
        //        ВидОперации = ВидыОперацийПоступлениеТоваровУслуг.Услуги,
        //        Контрагент = new Контрагенты
        //        {
        //            ИНН = "7711223344",
        //            Наименование = "ООО Тестовый контрагент",
        //        },
        //        Организация = store1C.Select<Организации>().Single(x => x.ИНН == organizationInn),
        //        СпособЗачетаАвансов = СпособыЗачетаАвансов.Автоматически,
        //        Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>
        //        {
        //            new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
        //            {
        //                Номенклатура = new Номенклатура
        //                {
        //                    Наименование = "стрижка"
        //                },
        //                Количество = 10,
        //                Содержание = "стрижка с кудряшками",
        //                Сумма = 120,
        //                Цена = 12,
        //                СтавкаНДС = СтавкиНДС.НДС18,
        //                СуммаНДС = 21.6m
        //            },
        //            new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
        //            {
        //                Номенклатура = new Номенклатура
        //                {
        //                    Наименование = "мытье головы"
        //                },
        //                Количество = 10,
        //                Содержание = "мытье головы хозяйственным мылом",
        //                Сумма = 120,
        //                Цена = 12,
        //                СтавкаНДС = СтавкиНДС.НДС18,
        //                СуммаНДС = 21.6m
        //            }
        //        }
        //    };

        //    store1C.Save(поступлениеТоваровУслуг);

        //    Assert.That(поступлениеТоваровУслуг.Номер, Is.Not.Null);
        //    Assert.That(поступлениеТоваровУслуг.Дата, Is.Not.EqualTo(default(DateTime)));
        //    Assert.That(поступлениеТоваровУслуг.Услуги.Count, Is.EqualTo(2));
        //    Assert.That(поступлениеТоваровУслуг.Услуги[0].НомерСтроки, Is.EqualTo(1));
        //    Assert.That(поступлениеТоваровУслуг.Услуги[1].НомерСтроки, Is.EqualTo(2));

        //    var valueTable = queryExecuter.Execute("Выбрать * ИЗ Документ.ПоступлениеТоваровУслуг ГДЕ Номер = &Number",
        //        new Dictionary<string, object>
        //        {
        //            {"Number", поступлениеТоваровУслуг.Номер}
        //        });

        //    Assert.That(valueTable.Count, Is.EqualTo(1));
        //    Assert.That(valueTable[0].GetString("НомерВходящегоДокумента"), Is.EqualTo("12345"));
        //    Assert.That(valueTable[0].GetDateTime("ДатаВходящегоДокумента"), Is.EqualTo(new DateTime(2016, 6, 1)));

        //    var servicesTablePart = valueTable[0]["Услуги"] as dynamic;
        //    Assert.That(servicesTablePart.Count, Is.EqualTo(2));

        //    var row1 = servicesTablePart.Получить(0);
        //    Assert.That(row1.Номенклатура.Наименование, Is.EqualTo("стрижка"));
        //    Assert.That(row1.Количество, Is.EqualTo(10));
        //    Assert.That(row1.Содержание, Is.EqualTo("стрижка с кудряшками"));
        //    Assert.That(row1.Сумма, Is.EqualTo(120));
        //    Assert.That(row1.Цена, Is.EqualTo(12));

        //    var row2 = servicesTablePart.Получить(1);
        //    Assert.That(row2.Номенклатура.Наименование, Is.EqualTo("мытье головы"));
        //    Assert.That(row2.Содержание, Is.EqualTo("мытье головы хозяйственным мылом"));
        //}

        //[Test]
        //public void CanModifyTableSection()
        //{
        //    var поступлениеТоваровУслуг = new ПоступлениеТоваровУслуг
        //    {
        //        ДатаВходящегоДокумента = new DateTime(2016, 6, 1),
        //        Дата = new DateTime(2016, 6, 1),
        //        НомерВходящегоДокумента = "12345",
        //        ВидОперации = ВидыОперацийПоступлениеТоваровУслуг.Услуги,
        //        Контрагент = new Контрагенты
        //        {
        //            ИНН = "7711223344",
        //            Наименование = "ООО Тестовый контрагент",
        //        },
        //        Организация = store1C.Select<Организации>().Single(x => x.ИНН == organizationInn),
        //        СпособЗачетаАвансов = СпособыЗачетаАвансов.Автоматически,
        //        Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>
        //        {
        //            new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
        //            {
        //                Номенклатура = new Номенклатура
        //                {
        //                    Наименование = "стрижка"
        //                },
        //                Количество = 10,
        //                Содержание = "стрижка с кудряшками",
        //                Сумма = 120,
        //                Цена = 12,
        //                СтавкаНДС = СтавкиНДС.НДС18,
        //                СуммаНДС = 21.6m
        //            }
        //        }
        //    };
        //    store1C.Save(поступлениеТоваровУслуг);

        //    поступлениеТоваровУслуг.Услуги[0].Содержание = "стрижка налысо";
        //    store1C.Save(поступлениеТоваровУслуг);

        //    var valueTable = queryExecuter.Execute("Выбрать * ИЗ Документ.ПоступлениеТоваровУслуг ГДЕ Номер = &Number",
        //        new Dictionary<string, object>
        //        {
        //            {"Number", поступлениеТоваровУслуг.Номер}
        //        });

        //    Assert.That(valueTable.Count, Is.EqualTo(1));

        //    var servicesTablePart = valueTable[0]["Услуги"] as dynamic;
        //    Assert.That(servicesTablePart.Count, Is.EqualTo(1));

        //    var row1 = servicesTablePart.Получить(0);
        //    Assert.That(row1.Содержание, Is.EqualTo("стрижка налысо"));
        //}

        //[Test]
        //public void CanDeleteItemFromTableSection()
        //{
        //    var поступлениеТоваровУслуг = new ПоступлениеТоваровУслуг
        //    {
        //        ДатаВходящегоДокумента = new DateTime(2016, 6, 1),
        //        Дата = new DateTime(2016, 6, 1),
        //        НомерВходящегоДокумента = "12345",
        //        ВидОперации = ВидыОперацийПоступлениеТоваровУслуг.Услуги,
        //        Контрагент = new Контрагенты
        //        {
        //            ИНН = "7711223344",
        //            Наименование = "ООО Тестовый контрагент",
        //        },
        //        Организация = store1C.Select<Организации>().Single(x => x.ИНН == organizationInn),
        //        СпособЗачетаАвансов = СпособыЗачетаАвансов.Автоматически,
        //        Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>
        //        {
        //            new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
        //            {
        //                Номенклатура = new Номенклатура
        //                {
        //                    Наименование = "стрижка"
        //                },
        //                Количество = 10,
        //                Содержание = "стрижка с кудряшками",
        //                Сумма = 120,
        //                Цена = 12,
        //                СтавкаНДС = СтавкиНДС.НДС18,
        //                СуммаНДС = 21.6m
        //            },
        //            new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
        //            {
        //                Номенклатура = new Номенклатура
        //                {
        //                    Наименование = "стрижка усов"
        //                },
        //                Количество = 10,
        //                Содержание = "стрижка бороды",
        //                Сумма = 120,
        //                Цена = 12,
        //                СтавкаНДС = СтавкиНДС.НДС18,
        //                СуммаНДС = 21.6m
        //            },
        //        }
        //    };
        //    store1C.Save(поступлениеТоваровУслуг);

        //    поступлениеТоваровУслуг.Услуги.RemoveAt(0);
        //    store1C.Save(поступлениеТоваровУслуг);

        //    var valueTable = queryExecuter.Execute("Выбрать * ИЗ Документ.ПоступлениеТоваровУслуг ГДЕ Номер = &Number",
        //        new Dictionary<string, object>
        //        {
        //            {"Number", поступлениеТоваровУслуг.Номер}
        //        });

        //    Assert.That(valueTable.Count, Is.EqualTo(1));

        //    var servicesTablePart = valueTable[0]["Услуги"] as dynamic;
        //    Assert.That(servicesTablePart.Count, Is.EqualTo(1));

        //    var row1 = servicesTablePart.Получить(0);
        //    Assert.That(row1.Содержание, Is.EqualTo("стрижка бороды"));
        //}

        //[Test]
        //public void CanChangeTableSectionItemsOrdering()
        //{
        //    var поступлениеТоваровУслуг = new ПоступлениеТоваровУслуг
        //    {
        //        ДатаВходящегоДокумента = new DateTime(2016, 6, 1),
        //        Дата = new DateTime(2016, 6, 1),
        //        НомерВходящегоДокумента = "12345",
        //        ВидОперации = ВидыОперацийПоступлениеТоваровУслуг.Услуги,
        //        Контрагент = new Контрагенты
        //        {
        //            ИНН = "7711223344",
        //            Наименование = "ООО Тестовый контрагент",
        //        },
        //        Организация = store1C.Select<Организации>().Single(x => x.ИНН == organizationInn),
        //        СпособЗачетаАвансов = СпособыЗачетаАвансов.Автоматически,
        //        Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>
        //        {
        //            new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
        //            {
        //                Номенклатура = new Номенклатура
        //                {
        //                    Наименование = "стрижка"
        //                },
        //                Количество = 10,
        //                Содержание = "стрижка с кудряшками",
        //                Сумма = 120,
        //                Цена = 12,
        //                СтавкаНДС = СтавкиНДС.НДС18,
        //                СуммаНДС = 21.6m
        //            },
        //            new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
        //            {
        //                Номенклатура = new Номенклатура
        //                {
        //                    Наименование = "стрижка усов"
        //                },
        //                Количество = 10,
        //                Содержание = "стрижка бороды",
        //                Сумма = 120,
        //                Цена = 12,
        //                СтавкаНДС = СтавкиНДС.НДС18,
        //                СуммаНДС = 21.6m
        //            },
        //        }
        //    };
        //    store1C.Save(поступлениеТоваровУслуг);

        //    var t = поступлениеТоваровУслуг.Услуги[0];
        //    поступлениеТоваровУслуг.Услуги[0] = поступлениеТоваровУслуг.Услуги[1];
        //    поступлениеТоваровУслуг.Услуги[1] = t;
        //    store1C.Save(поступлениеТоваровУслуг);

        //    var valueTable = queryExecuter.Execute("Выбрать * ИЗ Документ.ПоступлениеТоваровУслуг ГДЕ Номер = &Number",
        //        new Dictionary<string, object>
        //        {
        //            {"Number", поступлениеТоваровУслуг.Номер}
        //        });

        //    Assert.That(valueTable.Count, Is.EqualTo(1));

        //    var servicesTablePart = valueTable[0]["Услуги"] as dynamic;
        //    Assert.That(servicesTablePart.Count, Is.EqualTo(2));

        //    var row0 = servicesTablePart.Получить(0);
        //    Assert.That(row0.Содержание, Is.EqualTo("стрижка бороды"));

        //    var row1 = servicesTablePart.Получить(1);
        //    Assert.That(row1.Содержание, Is.EqualTo("стрижка с кудряшками"));
        //}

        //[Test]
        //public void ModifyReference()
        //{
        //    var counterpart = new Counterpart
        //    {
        //        Inn = "7711223344",
        //        Kpp = "771101001",
        //        FullName = "Test counterparty",
        //        Name = "Test counterparty"
        //    };
        //    var counterpartyAccessObject = counterpartManager.Create(counterpart);
        //    counterpart.Code = counterpartyAccessObject.Code;

        //    var counterpartContractAccessObject = counterpartContractManager.Create(counterpart,
        //        CounterpartContractKind.Incoming, "643");

        //    var contract = store1C.Select<ДоговорыКонтрагентов>()
        //        .Single(x => x.Код == counterpartContractAccessObject.Code);
        //    contract.Владелец.ИНН = "7711223344";
        //    contract.Владелец.Наименование = "Test counterparty 2";
        //    contract.Владелец.НаименованиеПолное = "Test counterparty 2";
        //    store1C.Save(contract);

        //    var valueTable = queryExecuter.Execute("ВЫБРАТЬ * ИЗ Справочник.Контрагенты ГДЕ ИНН=&Inn",
        //        new Dictionary<string, object>
        //        {
        //            {"Inn", "7711223344"}
        //        });
        //    Assert.That(valueTable.Count, Is.EqualTo(1));
        //    Assert.That(valueTable[0].GetString("Наименование"), Is.EqualTo("Test counterparty 2"));
        //}

        //public interface IGenericCatalog
        //{
        //    string Наименование { get; set; }
        //}

        //[Test]
        //public void CanQueryWithSourceNameViaGenericInterface()
        //{
        //    var counterpart = new Counterpart
        //    {
        //        Inn = "7711223344",
        //        Kpp = "771101001",
        //        FullName = "Test counterparty 1",
        //        Name = "Test counterparty 2"
        //    };
        //    counterpartManager.Create(counterpart);

        //    var catalogItem = store1C.Select<IGenericCatalog>("Справочник.Контрагенты")
        //        .Where(x => x.Наименование == "Test counterparty 2")
        //        .Cast<object>()
        //        .Single();
        //    Assert.That(catalogItem, Is.TypeOf<Контрагенты>());
        //}

        //private CounterpartAccessObject CreateTestCounterpart()
        //{
        //    var counterpart = new Counterpart
        //    {
        //        Name = "test-counterpart-name",
        //        Inn = "0987654321",
        //        Kpp = "987654321"
        //    };
        //    var counterpartAccessObject = counterpartManager.Create(counterpart);
        //    counterpart.Code = counterpartAccessObject.Code;
        //    bankAccountManager.CreateAccount(counterpartAccessObject.Code, BankAccountOwnerType.JuridicalCounterparty,
        //        new BankAccount
        //        {
        //            Bank = new Bank
        //            {
        //                Bik = Banks.AlfaBankBik
        //            },
        //            Number = "40702810001111122222",
        //            CurrencyCode = "643"
        //        });
        //    return counterpartAccessObject;
        //}
    }
}