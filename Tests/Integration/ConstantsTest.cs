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
    }
}