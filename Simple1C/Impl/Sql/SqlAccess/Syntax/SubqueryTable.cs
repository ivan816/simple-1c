using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class SubqueryTable : IColumnSource
    {
        public SubqueryClause Query { get; set; }
        public string Alias { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitSubqueryTable(this);
        }

        public override string ToString()
        {
            return string.Format("{0}. ({1}) as {2}",typeof(SubqueryTable).Name, Query, Alias);
        }
    }
}