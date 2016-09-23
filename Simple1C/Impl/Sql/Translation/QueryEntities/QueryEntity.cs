using System.Collections.Generic;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.QueryEntities
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
        public readonly List<QueryEntityProperty> properties = new List<QueryEntityProperty>();

        public ISqlElement unionCondition;
        public TableDeclarationClause declaration;

        public string GetAreaColumnName()
        {
            return GetSingleColumnName(PropertyNames.Area);
        }

        public string GetIdColumnName()
        {
            return GetSingleColumnName(PropertyNames.Id);
        }

        public string GetSingleColumnName(string propertyName)
        {
            return mapping.GetByPropertyName(propertyName).SingleLayout.DbColumnName;
        }
    }
}