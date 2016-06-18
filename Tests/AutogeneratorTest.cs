using Knopka.Application._1C.Mapper.Generation;
using NUnit.Framework;
using SimpleContainer.Infection;

namespace Knopka.Tests.Application._1C.Store1CTests
{
    public class AutogeneratorTest : IntegrationTestBase
    {
        [Inject] public DefaultGenerator generator;

        [Test]
        public void Test()
        {
            generator.Run();
        }
    }
}