using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.Translation;
using Simple1C.Tests.Helpers;

namespace Simple1C.Tests.Sql
{
    public abstract class TranslationTestBase : TestBase
    {
        protected DateTime? currentDate;

        protected override void SetUp()
        {
            base.SetUp();
            currentDate = null;
        }

        protected void CheckTranslate(string mappings, string sql, string expected, params int[] areas)
        {
            var inmemoryMappingStore = Parse(SpacesToTabs(mappings).Trim());
            var sqlTranslator = new QueryToSqlTranslator(inmemoryMappingStore, areas)
            {
                CurrentDate = currentDate
            };
            var translated = sqlTranslator.Translate(sql);
            var translatedLines = SpacesToTabs(translated.Trim())
                .Split(new[] {Environment.NewLine}, StringSplitOptions.None);
            var expectedLines = SpacesToTabs(expected.Trim())
                .Split(new[] {Environment.NewLine}, StringSplitOptions.None);

            Console.WriteLine("Input:\r\n{0}\r\n", sql);
            Console.WriteLine("Translated:\r\n{0}\r\n", translated);
            Console.WriteLine("Expected:\r\n{0}\r\n", expected);
            Assert.That(translatedLines, Is.EqualTo(expectedLines));
        }

        private static string SpacesToTabs(string s)
        {
            return s.Replace("    ", "\t");
        }

        private static InMemoryMappingStore Parse(string source)
        {
            var tableMappings = StringHelpers.ParseLinesWithTabs(source, delegate(string s, List<string> list)
            {
                var tableNames = s.Split(new[] {" "}, StringSplitOptions.None);
                return new TableMapping(tableNames[0], tableNames[1],
                    TableMapping.ParseTableType(tableNames[2]),
                    list.Select(PropertyMapping.Parse).ToArray());
            });
            return new InMemoryMappingStore(tableMappings.ToDictionary(x => x.QueryTableName,
                StringComparer.OrdinalIgnoreCase));
        }

        private class InMemoryMappingStore : IMappingSource
        {
            private readonly Dictionary<string, TableMapping> mappings;

            public InMemoryMappingStore(Dictionary<string, TableMapping> mappings)
            {
                this.mappings = mappings;
            }

            public TableMapping ResolveTableOrNull(string queryName)
            {
                return mappings.GetOrDefault(queryName);
            }
        }
    }
}