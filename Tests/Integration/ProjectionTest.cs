using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Simple1C.Interface;
using Simple1C.Tests.Metadata1C.Документы;
using Simple1C.Tests.Metadata1C.Перечисления;
using Simple1C.Tests.Metadata1C.ПланыСчетов;
using Simple1C.Tests.Metadata1C.Справочники;
using Simple1C.Tests.TestEntities;

namespace Simple1C.Tests.Integration
{
    internal class ProjectionTest : COMDataContextTestBase
    {
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
        public void SingleItemCalculatedExpression()
        {
            var контрагент = new Контрагенты
            {
                Наименование = "test contractor name",
                ИНН = "test-inn"
            };
            dataContext.Save(контрагент);
            var контрагент2 = dataContext.Select<Контрагенты>()
                .Where(x => x.ИНН == "test-inn")
                .Select(x => new
                {
                    projectedName = CalculateLocal(x.Наименование)
                })
                .Single();
            Assert.That(контрагент2.projectedName, Is.EqualTo("test contractor name_projected"));
        }

        private static string CalculateLocal(string name)
        {
            return name + "_projected";
        }

        [Test]
        public void ProjectionToAnonymousTypeWithConstants()
        {
            var контрагент = new Контрагенты
            {
                Наименование = "test contractor name",
                ИНН = "test-inn"
            };
            dataContext.Save(контрагент);
            var контрагент2 = dataContext.Select<Контрагенты>()
                .Where(x => x.Наименование == "test contractor name")
                .Select(x => new
                {
                    Контрагент_Инн = x.ИНН,
                    SomeConstant = GetConstant(),
                    SomeNullContant = (string) null
                })
                .ToArray();
            Assert.That(контрагент2.Length, Is.EqualTo(1));
            Assert.That(контрагент2[0].Контрагент_Инн, Is.EqualTo("test-inn"));
            Assert.That(контрагент2[0].SomeConstant, Is.EqualTo("test-constant"));
            Assert.That(контрагент2[0].SomeNullContant, Is.Null);
        }

        private static string GetConstant()
        {
            return "test-constant";
        }

        [Test]
        public void CanEvaluateExpressionsLocally()
        {
            var контрагент = new Контрагенты
            {
                Наименование = "test contractor name",
                ИНН = "test-inn"
            };
            dataContext.Save(контрагент);
            var selectedContractor = dataContext.Select<Контрагенты>()
                .Where(x => x.Наименование == "test contractor name")
                .Select(x => new
                {
                    x.Наименование,
                    НаименованиеИИнн = x.Наименование + "$$$" + x.ИНН
                })
                .ToArray()
                .Single();
            Assert.That(selectedContractor.Наименование, Is.EqualTo("test contractor name"));
            Assert.That(selectedContractor.НаименованиеИИнн, Is.EqualTo("test contractor name$$$test-inn"));
        }

        public class NameWithDescription
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }

        [Test]
        public void CanMapLocalExpresionToNamedType()
        {
            var контрагент1 = new Контрагенты
            {
                Наименование = "test-shortname1",
                ИНН = "test-inn1",
                КПП = "test-kpp",
                Комментарий = "test-comment1"
            };
            var контрагент2 = new Контрагенты
            {
                Наименование = "test-shortname2",
                НаименованиеПолное = "test-fullname2",
                ИНН = "test-inn2",
                КПП = "test-kpp",
                Комментарий = "test-comment2"
            };
            dataContext.Save(контрагент1);
            dataContext.Save(контрагент2);
            var selectedContractors = dataContext.Select<Контрагенты>()
                .Where(x => x.КПП == "test-kpp")
                .Select(x => new NameWithDescription
                {
                    Name = x.Наименование,
                    Description = (string.IsNullOrEmpty(x.НаименованиеПолное) ? x.Наименование : x.НаименованиеПолное)
                                  + "(" + x.ИНН + ") " + x.Комментарий
                })
                .OrderBy(x => x.Name)
                .ToArray();
            Assert.That(selectedContractors[0].Name, Is.EqualTo("test-shortname1"));
            Assert.That(selectedContractors[0].Description, Is.EqualTo("test-shortname1(test-inn1) test-comment1"));

            Assert.That(selectedContractors[1].Name, Is.EqualTo("test-shortname2"));
            Assert.That(selectedContractors[1].Description, Is.EqualTo("test-fullname2(test-inn2) test-comment2"));
        }

        [Test]
        public void CanUseLocalMethodsInProjection()
        {
            var контрагент1 = new Контрагенты
            {
                Наименование = "test-shortname1",
                КПП = "test-kpp"
            };

            dataContext.Save(контрагент1);
            var selectedContractors = dataContext.Select<Контрагенты>()
                .Where(x => x.КПП == "test-kpp")
                .Select(x => new
                {
                    GetWrap(x.Наименование).description
                })
                .Single();
            Assert.That(selectedContractors.description, Is.EqualTo("test-shortname1"));
        }

        private static DescriptionHolder GetWrap(string description)
        {
            return new DescriptionHolder {description = description};
        }

        private class DescriptionHolder
        {
            public string description;
        }

        [Test]
        public void CanSelectSameFieldWithDifferentAliases()
        {
            var счет2001 = dataContext.Single<Хозрасчетный>(x => x.Код == "20.01");
            var счет26 = dataContext.Single<Хозрасчетный>(x => x.Код == "26");
            var контрагент = new Контрагенты();
            var требованиеНакладная = new ТребованиеНакладная
            {
                Дата = new DateTime(2016, 6, 1),
                Контрагент = контрагент,
                СчетЗатрат = счет2001,
                Материалы = new List<ТребованиеНакладная.ТабличнаяЧастьМатериалы>
                {
                    new ТребованиеНакладная.ТабличнаяЧастьМатериалы
                    {
                        СчетЗатрат = счет26
                    }
                }
            };
            dataContext.Save(требованиеНакладная);

            var result = dataContext.Select<ТребованиеНакладная>()
                .Where(x => x.Контрагент == контрагент)
                .SelectMany(накладная => накладная.Материалы.Select(x =>
                    new
                    {
                        ItemCode = x.СчетЗатрат.Код,
                        DocCode = накладная.СчетЗатрат.Код
                    }))
                .Single();
            Assert.That(result.DocCode, Is.EqualTo("20.01"));
            Assert.That(result.ItemCode, Is.EqualTo("26"));
        }

        [Test]
        public void TypeOfAndPresentation()
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
                .Where(x => x.Владелец.ОсновнойБанковскийСчет.НомерСчета == "40702810001111122222")
                .Select(x => new
                {
                    OwnerType = x.Владелец.ОсновнойБанковскийСчет.Владелец.GetType(),
                    OwnerTypePresentation = Функции.Представление(x.Владелец.ОсновнойБанковскийСчет.Владелец.GetType())
                })
                .ToList();
            Assert.That(contractFromStore.Count, Is.EqualTo(1));
            Assert.That(contractFromStore[0].OwnerType, Is.EqualTo(typeof(Контрагенты)));
            Assert.That(contractFromStore[0].OwnerTypePresentation, Is.EqualTo("Контрагент"));
        }

        [Test]
        public void RerefenceProjection()
        {
            var counterpart = new Counterpart
            {
                Name = "test-counterpart-name",
                Inn = "0987654321",
                Kpp = "987654321"
            };
            dynamic counterpartAccessObject = testObjectsManager.CreateCounterparty(counterpart);
            string code = counterpartAccessObject.Код;
            var counterparties = dataContext.Select<Контрагенты>()
                .Where(x => x.Код == code)
                .Select(x => new
                {
                    Ссылка = x
                })
                .ToArray();

            Assert.That(counterparties.Length, Is.EqualTo(1));
            Assert.That(counterparties[0].Ссылка.Наименование, Is.EqualTo("test-counterpart-name"));
        }

        [Test]
        public void ComplexRerefenceProjection()
        {
            var counterpart = new Counterpart
            {
                Name = "test-counterpart-name",
                Inn = "0987654321",
                Kpp = "987654321"
            };
            dynamic counterpartAccessObject = testObjectsManager.CreateCounterparty(counterpart);
            string code = counterpartAccessObject.Код;
            var counterparties = dataContext.Select<Контрагенты>()
                .Where(x => x.Код == code)
                .Select(x => new
                {
                    Ссылка = x,
                    x.ИНН
                })
                .ToArray();

            Assert.That(counterparties.Length, Is.EqualTo(1));
            Assert.That(counterparties[0].Ссылка.Наименование, Is.EqualTo("test-counterpart-name"));
            Assert.That(counterparties[0].ИНН, Is.EqualTo("0987654321"));
        }

        [Test]
        public void ComplexReferenceObjectProjection()
        {
            var counterpart = new Counterpart
            {
                Name = "test-counterpart-name",
                Inn = "0987654321",
                Kpp = "987654321"
            };
            testObjectsManager.CreateCounterparty(counterpart);
            var counterparties = dataContext.Select<Контрагенты>()
                .Where(x => !x.ПометкаУдаления)
                .Where(x => !x.ЭтоГруппа)
                .Where(x => x.Наименование == "test-counterpart-name")
                .Select(x => new
                {
                    Id = x.УникальныйИдентификатор,
                    Reference = x
                })
                .ToArray();
            Assert.That(counterparties.Length, Is.EqualTo(1));
            Assert.That(counterparties[0].Reference, Is.TypeOf<Контрагенты>());
        }

        [Test]
        public void TakeThenSelectMany()
        {
            var контрагент = new Контрагенты
            {
                Наименование = "test contractor name",
                ИНН = "test-inn"
            };
            dataContext.Save(new ПоступлениеТоваровУслуг
            {
                Комментарий = "test1",
                Дата = new DateTime(2016, 6, 1),
                Контрагент = контрагент,
                Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>
                {
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура {Наименование = "стул"}
                    },
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура {Наименование = "стол"}
                    }
                }
            }, new ПоступлениеТоваровУслуг
            {
                Комментарий = "test2",
                Дата = new DateTime(2016, 6, 1),
                Контрагент = контрагент,
                Услуги = new List<ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги>
                {
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура {Наименование = "яблоко"}
                    },
                    new ПоступлениеТоваровУслуг.ТабличнаяЧастьУслуги
                    {
                        Номенклатура = new Номенклатура {Наименование = "апельсин"}
                    }
                }
            });
            var selected = dataContext.Select<ПоступлениеТоваровУслуг>()
                .OrderBy(x => x.Комментарий)
                .Take(1)
                .Select(x => new
                {
                    comment = x.Комментарий,
                    services = x.Услуги
                })
                .SelectMany(x => x.services.Select(y => new
                {
                    x.comment,
                    nomenclature = y.Номенклатура.Наименование
                }))
                .OrderBy(x => x.nomenclature)
                .ToArray();

            Assert.That(selected.Length, Is.EqualTo(2));
            Assert.That(selected[0].comment, Is.EqualTo("test1"));
            Assert.That(selected[0].nomenclature, Is.EqualTo("стол"));
            Assert.That(selected[1].comment, Is.EqualTo("test2"));
            Assert.That(selected[1].nomenclature, Is.EqualTo("стул"));
        }

        [Test]
        public void ProjectionFilter()
        {
            var contractor1 = new Контрагенты
            {
                Наименование = "test-name",
                ИНН = "inn1",
                КПП = "kpp1"
            };
            dataContext.Save(contractor1);

            var selected = dataContext.Select<Контрагенты>()
                .Select(x => new
                {
                    name = x.Наименование,
                    inn = x.ИНН
                })
                .Where(x => x.inn == "inn1")
                .ToArray();
            Assert.That(selected.Length, Is.EqualTo(1));
            Assert.That(selected[0].name, Is.EqualTo("test-name"));
        }

        [Test]
        public void CanUseContainsMethod()
        {
            var contractor1 = new Контрагенты
            {
                Наименование = "test-name",
                ИНН = "inn1",
                КПП = "kpp1"
            };
            dataContext.Save(contractor1);

            var contractor2 = new Контрагенты
            {
                Наименование = "test-name",
                ИНН = "inn2",
                КПП = "kpp2"
            };
            dataContext.Save(contractor2);
            var contractorIdSet = new HashSet<Guid>
            {
                contractor2.УникальныйИдентификатор.GetValueOrDefault()
            };

            var contractors = dataContext.Select<Контрагенты>()
                .Where(x => x.Наименование == "test-name")
                .Select(x => new
                {
                    inn = x.ИНН,
                    isInSet = contractorIdSet.Contains(x.УникальныйИдентификатор.GetValueOrDefault())
                })
                .ToArray();
            Assert.That(contractors.Length, Is.EqualTo(2));
            Assert.That(contractors[0].inn, Is.EqualTo("inn1"));
            Assert.That(contractors[0].isInSet, Is.False);
            Assert.That(contractors[1].inn, Is.EqualTo("inn2"));
            Assert.That(contractors[1].isInSet, Is.True);
        }

        [Test]
        public void DbNullInUniqueIdentifier()
        {
            var counterpart = new Контрагенты
            {
                Наименование = "test-counterpart-name",
                ИНН = "0987654321",
                КПП = "987654321"
            };
            dataContext.Save(counterpart);
            var договор = new ДоговорыКонтрагентов
            {
                ВидДоговора = ВидыДоговоровКонтрагентов.Прочее,
                Владелец = counterpart,
                Наименование = "Основной договор"
            };
            dataContext.Save(договор);

            var counterparties = dataContext.Select<ДоговорыКонтрагентов>()
                .Where(x => x.УникальныйИдентификатор == договор.УникальныйИдентификатор)
                .Select(x => new
                {
                    Id = x.Организация.УникальныйИдентификатор
                })
                .ToArray();
            Assert.That(counterparties.Length, Is.EqualTo(1));
            Assert.That(counterparties[0].Id, Is.Null);
        }

        [Test]
        public void SinglePropertyProjection()
        {
            var контрагент = new Контрагенты
            {
                Наименование = "test contractor name",
                ИНН = "test-inn"
            };
            dataContext.Save(контрагент);

            var инн = dataContext
                .Select<Контрагенты>()
                .Where(x => x.Наименование == "test contractor name")
                .Select(x => x.ИНН)
                .FirstOrDefault();
            Assert.That(инн, Is.EqualTo("test-inn"));
        }
    }
}