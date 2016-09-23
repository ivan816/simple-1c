using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Simple1C.Interface;
using Simple1C.Tests.Metadata1C.Документы;
using Simple1C.Tests.Metadata1C.Перечисления;
using Simple1C.Tests.Metadata1C.Справочники;
using Simple1C.Tests.TestEntities;

namespace Simple1C.Tests.Integration
{
    internal class TableSectionsTest : COMDataContextTestBase
    {
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
    }
}