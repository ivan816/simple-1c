using System;
using System.Linq;
using NUnit.Framework;
using Simple1C.Interface;
using Simple1C.Tests.Metadata1C.РегистрыСведений;
using Simple1C.Tests.Metadata1C.Справочники;

namespace Simple1C.Tests.Integration
{
    internal class InformationRegisterTest : COMDataContextTestBase
    {
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
        public void CanQueryCanUpdateManyItems()
        {
            var валюта = dataContext.Single<Валюты>(x => x.Код == "643");
            dataContext.Save(new КурсыВалют
            {
                Валюта = валюта,
                Кратность = 42,
                Курс = 12,
                Период = new DateTime(2025, 7, 18)
            }, new КурсыВалют
            {
                Валюта = валюта,
                Кратность = 42,
                Курс = 13,
                Период = new DateTime(2025, 7, 19)
            }, new КурсыВалют
            {
                Валюта = валюта,
                Кратность = 42,
                Курс = 14,
                Период = new DateTime(2025, 7, 20)
            });

            var курсыВалют = dataContext.Select<КурсыВалют>()
                .Where(x => x.Период > new DateTime(2025, 7, 1))
                .OrderBy(x => x.Период)
                .ToArray();
            Assert.That(курсыВалют[0].Курс, Is.EqualTo(12));
            Assert.That(курсыВалют[1].Курс, Is.EqualTo(13));
            Assert.That(курсыВалют[2].Курс, Is.EqualTo(14));

            курсыВалют[0].Курс = 120;
            курсыВалют[1].Курс = 130;
            курсыВалют[2].Курс = 140;
            dataContext.Save(курсыВалют);
            var курсыВалют2 = dataContext.Select<КурсыВалют>()
                .Where(x => x.Период > new DateTime(2025, 7, 1))
                .OrderBy(x => x.Период)
                .ToArray();
            Assert.That(курсыВалют2[0].Курс, Is.EqualTo(120));
            Assert.That(курсыВалют2[1].Курс, Is.EqualTo(130));
            Assert.That(курсыВалют2[2].Курс, Is.EqualTo(140));
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
    }
}