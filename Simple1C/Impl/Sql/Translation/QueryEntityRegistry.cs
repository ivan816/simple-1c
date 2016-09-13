using System;
using System.Collections.Generic;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation
{
    internal class QueryEntityRegistry
    {
        private readonly Dictionary<TableDeclarationClause, QueryRoot> queryTables =
            new Dictionary<TableDeclarationClause, QueryRoot>();

        private readonly IMappingSource mappingSource;

        public QueryEntityRegistry(IMappingSource mappingSource)
        {
            this.mappingSource = mappingSource;
        }

        public void Register(TableDeclarationClause declaration)
        {
            var queryEntity = CreateQueryEntity(null, declaration.Name);
            var mainQueryEntity = new QueryRoot(queryEntity, declaration);
            queryTables.Add(declaration, mainQueryEntity);
        }

        public QueryRoot Get(TableDeclarationClause declaration)
        {
            QueryRoot root;
            if (!queryTables.TryGetValue(declaration, out root))
            {
                const string messageFormat = "can't find query table for [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, declaration.GetRefName()));
            }
            return root;
        }

        public QueryEntity CreateQueryEntity(QueryEntityProperty referer, string queryName)
        {
            var tableMapping = mappingSource.ResolveTable(queryName);
            return new QueryEntity(tableMapping, referer);
        }
    }
}