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
            var mappings = @"A a
    c1 cc1
    c2 cc2
B b
C c
    v1 vv1".Replace("    ", "\t");

            var mappingSchema = MappingSchema.Parse(mappings);
            Assert.That(mappingSchema.Tables.Length, Is.EqualTo(3));
            Assert.That(mappingSchema.Tables[0].Columns.Length, Is.EqualTo(2));
            Assert.That(mappingSchema.Tables[0].Columns[0].QueryName, Is.EqualTo("c1"));
            Assert.That(mappingSchema.Tables[0].Columns[0].DbName, Is.EqualTo("cc1"));
            Assert.That(mappingSchema.Tables[0].Columns[1].QueryName, Is.EqualTo("c2"));
            Assert.That(mappingSchema.Tables[0].Columns[1].DbName, Is.EqualTo("cc2"));
            Assert.That(mappingSchema.Tables[1].Columns.Length, Is.EqualTo(0));
            Assert.That(mappingSchema.Tables[2].Columns.Length, Is.EqualTo(1));
            Assert.That(mappingSchema.Tables[2].Columns[0].QueryName, Is.EqualTo("v1"));
            Assert.That(mappingSchema.Tables[2].Columns[0].DbName, Is.EqualTo("vv1"));
        }
    }
}