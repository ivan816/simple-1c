using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class UnionClause : ISqlElement
    {
        public SelectClause SelectClause { get; set; }
        public UnionType? Type { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitUnion(this);
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", SelectClause, Type == null ? "" : Type.ToString());
        }
    }
}