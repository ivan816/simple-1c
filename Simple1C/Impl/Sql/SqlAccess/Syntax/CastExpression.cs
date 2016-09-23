using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class CastExpression : ISqlElement
    {
        public string Type { get; set; }
        public ISqlElement Expression { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitCast(this);
        }
    }
}