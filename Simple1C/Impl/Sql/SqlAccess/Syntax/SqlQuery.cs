using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class SqlQuery : ISqlElement
    {
        public SqlQuery()
        {
            Unions = new List<UnionClause>();
        }

        public List<UnionClause> Unions { get; set; }
        public OrderByClause OrderBy { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitSqlQuery(this);
        }

        public SelectClause GetSingleSelect()
        {
            var union = Unions.Single();
            if (union.Type.HasValue)
                throw new InvalidOperationException("Assertion failure. Expected single select without unions");
            return union.SelectClause;
        }
    }
}