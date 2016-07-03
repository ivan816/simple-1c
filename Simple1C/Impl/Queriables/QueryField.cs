using System.Collections.Generic;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Queriables
{
    internal class QueryField
    {
        public QueryField(string sourceName, List<string> pathItems)
        {
            PathItems = pathItems.ToArray();
            List<int> uniqueIdentifierFieldIndexesList = null;
            for (var i = 0; i < PathItems.Length; i++)
            {
                var fieldName = PathItems[i];
                if (fieldName == EntityHelpers.idPropertyName)
                {
                    if (uniqueIdentifierFieldIndexesList == null)
                        uniqueIdentifierFieldIndexesList = new List<int>();
                    uniqueIdentifierFieldIndexesList.Add(i);
                    PathItems[i] = "Ссылка";
                }
            }
            if (uniqueIdentifierFieldIndexesList != null)
                UniqueIdentifierFieldIndexes = uniqueIdentifierFieldIndexesList.ToArray();
            Path = PathItems.JoinStrings(".");
            Expression = sourceName + "." + Path;
            var aliasItems = new string[PathItems.Length];
            for (var i = 0; i < PathItems.Length; i++)
            {
                var item = PathItems[i];
                aliasItems[i] = item == "Количество" ? "Quantity" : item;
            }
            Alias = aliasItems.JoinStrings("_");
        }
        public int[] UniqueIdentifierFieldIndexes { get; private set; }
        public string[] PathItems { get; private set; }
        public string Path { get; private set; }
        public string Alias { get; private set; }
        public string Expression { get; private set; }
    }
}