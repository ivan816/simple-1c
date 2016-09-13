using System.Collections.Generic;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation
{
    internal class QueryEntity
    {
        public QueryEntity(TableMapping mapping, QueryEntityProperty referer)
        {
            this.mapping = mapping;
            this.referer = referer;
        }

        public readonly TableMapping mapping;
        public readonly QueryEntityProperty referer;
        public TableDeclarationClause declaration;
        public readonly List<QueryEntityProperty> properties = new List<QueryEntityProperty>();
        public ISqlElement unionCondition;

        public string GetAreaColumnName()
        {
            return GetSingleColumnName("ќбластьƒанныхќсновныеƒанные");
        }

        public string GetIdColumnName()
        {
            return GetSingleColumnName("—сылка");
        }

        public string GetSingleColumnName(string propertyName)
        {
            return mapping.GetByPropertyName(propertyName).SingleLayout.ColumnName;
        }
    }
}