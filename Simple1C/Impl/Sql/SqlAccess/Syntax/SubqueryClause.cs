using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class SubqueryClause : ISqlElement
    {
        public SqlQuery Query { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitSubquery(this);
        }

        public override string ToString()
        {
            return string.Format("{0}. ({1})", typeof (SubqueryClause), Query);
        }
    }
}