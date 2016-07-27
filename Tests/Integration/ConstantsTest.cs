using System;
using NUnit.Framework;
using Simple1C.Interface;
using Simple1C.Tests.Metadata1C.Константы;

namespace Simple1C.Tests.Integration
{
    internal class ConstantsTest : COMDataContextTestBase
    {
        [Test]
        public void Simple()
        {
            Assert.That(dataContext.Single<ИспользоватьДатыЗапретаИзменения>().Значение, Is.False);
            dataContext.Save(new ИспользоватьДатыЗапретаИзменения {Значение = true});
            Assert.That(dataContext.Single<ИспользоватьДатыЗапретаИзменения>().Значение, Is.True);
            dataContext.Save(new ИспользоватьДатыЗапретаИзменения {Значение = false});
            Assert.That(dataContext.Single<ИспользоватьДатыЗапретаИзменения>().Значение, Is.False);
        }

        [Test]
        public void CanSaveGuidConstant()
        {
            var value = Guid.NewGuid();
            dataContext.Save(new ВерсияДатЗапретаИзменения {Значение = value});
            Assert.That(dataContext.Single<ВерсияДатЗапретаИзменения>().Значение.GetValueOrDefault(),
                Is.EqualTo(value));
            dataContext.Save(new ВерсияДатЗапретаИзменения {Значение = null});
            Assert.That(dataContext.Single<ВерсияДатЗапретаИзменения>().Значение, Is.Null);
        }
    }
}