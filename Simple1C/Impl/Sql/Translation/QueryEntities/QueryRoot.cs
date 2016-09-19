using System.Collections.Generic;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.QueryEntities
{
    internal class QueryRoot
    {
        public readonly QueryEntity entity;
        public readonly IColumnSource tableDeclaration;
        public readonly Dictionary<string, QueryField> fields = new Dictionary<string, QueryField>();
        public bool subqueryRequired;

        public QueryRoot(QueryEntity entity, IColumnSource tableDeclaration)
        {
            this.entity = entity;
            this.tableDeclaration = tableDeclaration;
        }
    }
}