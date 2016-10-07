using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class IsReferenceExpression : ISqlElement
    {
        public ColumnReferenceExpression Argument { get; set; }
        public string ObjectName { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitIsReference(this);
        }
    }
}