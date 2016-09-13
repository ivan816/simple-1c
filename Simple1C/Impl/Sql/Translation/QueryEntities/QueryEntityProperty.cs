using System.Collections.Generic;
using Simple1C.Impl.Sql.SchemaMapping;

namespace Simple1C.Impl.Sql.Translation.QueryEntities
{
    internal class QueryEntityProperty
    {
        public readonly QueryEntity referer;
        public readonly PropertyMapping mapping;
        public readonly List<QueryEntity> nestedEntities = new List<QueryEntity>();
        public bool referenced;

        public QueryEntityProperty(QueryEntity referer, PropertyMapping mapping)
        {
            this.referer = referer;
            this.mapping = mapping;
        }

        public string GetDbColumnName()
        {
            return mapping.SingleLayout != null
                ? mapping.SingleLayout.ColumnName
                : mapping.UnionLayout.ReferenceColumnName;
        }
    }
}