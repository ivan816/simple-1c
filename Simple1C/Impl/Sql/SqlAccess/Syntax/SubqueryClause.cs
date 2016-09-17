using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class SubqueryClause : IColumnSource
    {
        public SqlQuery Query { get; set; }
        public string Alias { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitSubquery(this);
        }

        public override string ToString()
        {
            return string.Format("({0} as {1})", Query, Alias);
        }
    }
}