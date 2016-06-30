using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Queriables
{
    internal class QueryField
    {
        public QueryField(string sourceName, string[] pathItems)
        {
            PathItems = pathItems;
            Path = PathItems.JoinStrings(".");
            Expression = sourceName + "." + Path;
            var aliasItems = new string[pathItems.Length];
            for (var i = 0; i < pathItems.Length; i++)
            {
                var item = pathItems[i];
                aliasItems[i] = item == "Количество" ? "Quantity" : item;
            }
            Alias = aliasItems.JoinStrings("_");
        }

        public string[] PathItems { get; private set; }
        public string Path { get; private set; }
        public string Alias { get; private set; }
        public string Expression { get; private set; }
    }
}