using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class IsNullExpression : ISqlElement
    {
        public ISqlElement Argument { get; set; }
        public bool IsNotNull { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitIsNullExpression(this);
        }
    }
}