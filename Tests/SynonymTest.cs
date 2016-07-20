using NUnit.Framework;
using Simple1C.Interface;
using Simple1C.Tests.Helpers;
using Simple1C.Tests.Metadata1C.Перечисления;

namespace Simple1C.Tests
{
    public class SynonymTest : TestBase
    {
        [Test]
        public void CanReadSynonyms()
        {
            Assert.That(Synonym.Of(ВидыРасходовНУ.АмортизационнаяПремия), Is.EqualTo("Амортизационная премия"));
        }
    }
}