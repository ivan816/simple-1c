using System;
using System.Collections.Generic;
using System.Text;
using Simple1C.Impl.Com;

namespace Simple1C.Impl.Queriables
{
    internal class QueryField
    {
        private readonly bool isUniqueIdentifier;

        public QueryField(string sourceName, List<string> pathItems, bool needPresentation, bool needType, Type type)
        {
            Type = type;
            PathItems = pathItems.ToArray();
            isUniqueIdentifier = PathItems.Length > 0 &&
                                 PathItems[PathItems.Length - 1] == EntityHelpers.idPropertyName;
            if (isUniqueIdentifier)
                PathItems[PathItems.Length - 1] = "Ссылка";

            var expressionBuilder = new StringBuilder();
            var aliasBuilder = new StringBuilder();
            if (needPresentation)
                expressionBuilder.Append("ПРЕДСТАВЛЕНИЕ(");
            if (needType)
                expressionBuilder.Append("ТИПЗНАЧЕНИЯ(");
            expressionBuilder.Append(sourceName);
            aliasBuilder.Append(sourceName);
            foreach (var item in PathItems)
            {
                expressionBuilder.Append(".");
                expressionBuilder.Append(item);
                aliasBuilder.Append("_");
                aliasBuilder.Append(item);
            }
            if (needType)
            {
                expressionBuilder.Append(")");
                aliasBuilder.Append("_ТИПЗНАЧЕНИЯ");
            }
            if (needPresentation)
            {
                expressionBuilder.Append(")");
                aliasBuilder.Append("_ПРЕДСТАВЛЕНИЕ");
            }
            Expression = expressionBuilder.ToString();
            Alias = aliasBuilder.Replace('.', '_').ToString();
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