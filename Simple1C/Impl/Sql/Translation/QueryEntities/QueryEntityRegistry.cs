using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.QueryEntities
{
    internal class QueryEntityRegistry
    {
        private readonly Dictionary<IColumnSource, QueryRoot> queryTables =
            new Dictionary<IColumnSource, QueryRoot>();

        private readonly IMappingSource mappingSource;

        public QueryEntityRegistry(IMappingSource mappingSource)
        {
            this.mappingSource = mappingSource;
        }

        public void RegisterTable(TableDeclarationClause declaration)
        {
            QueryRoot queryRoot;
            if (queryTables.TryGetValue(declaration, out queryRoot))
                return;
            var queryEntity = CreateQueryEntity(null, declaration.Name);
            queryRoot = new QueryRoot(queryEntity, declaration);
            queryTables.Add(declaration, queryRoot);
        }

        public void RegisterSubquery(SubqueryTable clause)
        {
            QueryRoot queryRoot;
            if (queryTables.TryGetValue(clause, out queryRoot))
                return;
            var subqueryProperties = CreateSubqueryProperties(clause.Query.Query.Unions.First().SelectClause);
            var mapping = new TableMapping(clause.Alias, clause.Alias, TableType.Main, subqueryProperties);
            var queryEntity = new QueryEntity(mapping, null);
            queryRoot = new QueryRoot(queryEntity, clause);
            queryTables.Add(clause, queryRoot);
        }

        public QueryRoot Get(IColumnSource declaration)
        {
            QueryRoot root;
            if (!queryTables.TryGetValue(declaration, out root))
            {
                const string messageFormat = "can't find query table for [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, declaration));
            }
            return root;
        }

        public QueryEntity CreateQueryEntity(QueryEntityProperty referer, string queryName)
        {
            var tableMapping = mappingSource.ResolveTableOrNull(queryName);
            if (tableMapping == null)
            {
                const string messageFormat = "can't find table mapping for [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, queryName));
            }
            return new QueryEntity(tableMapping, referer);
        }

        private PropertyMapping[] CreateSubqueryProperties(SelectClause clause)
        {
            if (clause.IsSelectAll)
                return Get(clause.Source).entity.mapping.Properties;
            return clause.Fields.Select(c =>
            {
                if (!string.IsNullOrEmpty(c.Alias))
                    return new PropertyMapping(c.Alias, new SingleLayout(c.Alias, null), null);
                var columnReference = ExtractColumnReferenceOrDie(c);
                return Get(columnReference.Table).entity.mapping.GetByPropertyName(columnReference.Name);
            }).ToArray();
        }

        private static ColumnReferenceExpression ExtractColumnReferenceOrDie(SelectFieldExpression c)
        {
            var result = c.Expression as ColumnReferenceExpression;
            if (result == null)
            {
                const string messageFormat = "could not determine subquery property name from expression [{0}]";
                throw new NotSupportedException(string.Format(messageFormat, c.Expression));
            }
            return result;
        }
    }
}