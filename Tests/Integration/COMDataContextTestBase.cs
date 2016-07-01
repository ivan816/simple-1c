using System;
using System.Collections.Generic;
using NUnit.Framework;
using Simple1C.Interface;
using Simple1C.Tests.Metadata1C.Документы;
using Simple1C.Tests.Metadata1C.Перечисления;
using Simple1C.Tests.Metadata1C.ПланыСчетов;
using Simple1C.Tests.Metadata1C.Справочники;
using Simple1C.Tests.TestEntities;

namespace Simple1C.Tests.Integration
{
    internal abstract class COMDataContextTestBase : IntegrationTestBase
    {
        protected IDataContext dataContext;
        protected internal TestObjectsManager testObjectsManager;
        protected EnumConverter enumConverter;

        protected override void SetUp()
        {
            base.SetUp();
            dataContext = DataContextFactory.CreateCOM(globalContext.ComObject(), typeof(Контрагенты).Assembly);
            enumConverter = new EnumConverter(globalContext);
            testObjectsManager = new TestObjectsManager(globalContext, enumConverter, organizationInn);
        }

        protected Организации ПолучитьТекущуюОрганизацию()
        {
            return dataContext.Single<Организации>(
                x => !x.ПометкаУдаления,
                x => x.ИНН == organizationInn);
        }

        protected ПоступлениеТоваровУслуг CreateFullFilledDocument()
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

        protected ПодразделенияОрганизаций ПолучитьОсновноеПодразделение()
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

        protected dynamic GetDocumentByNumber(string number)
        {
            var valueTable = globalContext.Execute("Выбрать * ИЗ Документ.ПоступлениеТоваровУслуг ГДЕ Номер = &Number",
                new Dictionary<string, object>
                {
                    {"Number", number}
                }).Unload();
            Assert.That(valueTable.Count, Is.EqualTo(1));
            return valueTable[0]["Ссылка"];
        }

        protected object CreateTestCounterpart()
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