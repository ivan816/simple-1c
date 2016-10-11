using System;
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
            var result = mapping.SingleLayout != null
                ? mapping.SingleLayout.DbColumnName
                : mapping.UnionLayout.ReferenceColumnName;
            if (string.IsNullOrEmpty(result))
            {
                const string messageFormat = "ref column is not defined for [{0}.{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    referer.mapping.QueryTableName, mapping.PropertyName));
            }
            return result;
        }
    }
}