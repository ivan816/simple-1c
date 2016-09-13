using System.Collections.Generic;

namespace Simple1C.Impl.Sql.Translation.QueryEntities
{
    internal class NameGenerator
    {
        private readonly Dictionary<string, int> lastUsed = new Dictionary<string, int>();

        public string GenerateTableName()
        {
            return Generate("__nested_table");
        }

        public void Reset()
        {
            lastUsed.Clear();
        }

        public string GenerateColumnName()
        {
            return Generate("__nested_field");
        }

        private string Generate(string prefix)
        {
            int lastUsedForPrefix;
            var number =
                lastUsed[prefix] = lastUsed.TryGetValue(prefix, out lastUsedForPrefix) ? lastUsedForPrefix + 1 : 0;
            return prefix + number;
        }
    }
}