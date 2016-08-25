using NUnit.Framework;
using Simple1C.Impl.Sql;
using Simple1C.Tests.Helpers;

namespace Simple1C.Tests.Sql
{
    public class MappingSchemaParsingTest : TestBase
    {
        [Test]
        public void Simple()
        {
            var mappings = @"A.A1 a
    c1 cc1
    c2 cc2
B.B1 b
C.C1 c
    v1 vv1".Replace("    ", "\t");

            var mappingSchema = InMemoryMappingStore.Parse(mappings);
            Assert.That(mappingSchema.Tables.Length, Is.EqualTo(3));
            Assert.That(mappingSchema.Tables[0].Properties.Length, Is.EqualTo(2));
            Assert.That(mappingSchema.Tables[0].Properties[0].PropertyName, Is.EqualTo("c1"));
            Assert.That(mappingSchema.Tables[0].Properties[0].FieldName, Is.EqualTo("_cc1"));
            Assert.That(mappingSchema.Tables[0].Properties[1].PropertyName, Is.EqualTo("c2"));
            Assert.That(mappingSchema.Tables[0].Properties[1].FieldName, Is.EqualTo("_cc2"));
            Assert.That(mappingSchema.Tables[1].Properties.Length, Is.EqualTo(0));
            Assert.That(mappingSchema.Tables[2].Properties.Length, Is.EqualTo(1));
            Assert.That(mappingSchema.Tables[2].Properties[0].PropertyName, Is.EqualTo("v1"));
            Assert.That(mappingSchema.Tables[2].Properties[0].FieldName, Is.EqualTo("_vv1"));
        }
    }
}