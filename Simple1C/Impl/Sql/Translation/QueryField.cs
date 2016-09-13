using System.Collections.Generic;

namespace Simple1C.Impl.Sql.Translation
{
    internal class QueryField
    {
        public readonly string alias;
        public readonly QueryEntityProperty[] properties;
        public readonly bool invert;

        public QueryField(string alias, QueryEntityProperty[] properties, bool invert)
        {
            this.alias = alias;
            this.properties = properties;
            this.invert = invert;
        }

        public readonly List<SelectPart> parts = new List<SelectPart>();
    }
}