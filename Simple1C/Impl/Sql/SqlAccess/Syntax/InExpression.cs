using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class InExpression : ISqlElement
    {
        public ColumnReferenceExpression Column { get; set; }
        public ISqlElement Source { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitIn(this);
        }
    }
}