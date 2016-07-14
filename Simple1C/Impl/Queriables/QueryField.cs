using System;
using System.Collections.Generic;
using Simple1C.Impl.Com;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Queriables
{
    internal class QueryField
    {
        private readonly bool isUniqueIdentifier;

        public QueryField(string sourceName, List<string> pathItems, Type type)
        {
            Type = type;
            PathItems = pathItems.ToArray();
            isUniqueIdentifier = PathItems[PathItems.Length - 1] == EntityHelpers.idPropertyName;
            if (isUniqueIdentifier)
                PathItems[PathItems.Length - 1] = "—сылка";
            Expression = sourceName + "." + PathItems.JoinStrings(".");
            Alias = sourceName.Replace('.', '_') + "_" + PathItems.JoinStrings("_");
        }

        public object GetValue(object queryResultRow)
        {
            var result = ComHelpers.GetProperty(queryResultRow, Alias);
            if (isUniqueIdentifier)
                result = ComHelpers.Invoke(result, EntityHelpers.idPropertyName);
            return result;
        }

        public string[] PathItems { get; private set; }
        public string Alias { get; private set; }
        public string Expression { get; private set; }
        public Type Type { get; private set; }
    }
}