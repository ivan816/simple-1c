using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class SelectFieldExpression : ISqlElement
    {
        public ISqlElement Expression { get; set; }
        public string Alias { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitSelectField(this);
        }

        public override string ToString()
        {
            return string.Format("{0}. {1} as {2}", typeof(SelectFieldExpression).Name, Expression, Alias);
        }
    }
}