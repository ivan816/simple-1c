using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Simple1C.Interface;
using Simple1C.Tests.Metadata1C.Документы;
using Simple1C.Tests.Metadata1C.ПланыСчетов;
using Simple1C.Tests.Metadata1C.Справочники;

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

        private class NameWithDescription
        {
            public string name;
            public string description;
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
                    name = x.Наименование,
                    description = (string.IsNullOrEmpty(x.НаименованиеПолное) ? x.Наименование : x.НаименованиеПолное)
                                  + "(" + x.ИНН + ") " + x.Комментарий
                })
                .OrderBy(x => x.name)
                .ToArray();
            Assert.That(selectedContractors[0].name, Is.EqualTo("test-shortname1"));
            Assert.That(selectedContractors[0].description, Is.EqualTo("test-shortname1(test-inn1) test-comment1"));

            Assert.That(selectedContractors[1].name, Is.EqualTo("test-shortname2"));
            Assert.That(selectedContractors[1].description, Is.EqualTo("test-fullname2(test-inn2) test-comment2"));
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
                Материалы = new List<ТребованиеНакладная.ТабличнаяЧастьМатериалы>()
                {
                    new ТребованиеНакладная.ТабличнаяЧастьМатериалы()
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
    }
}