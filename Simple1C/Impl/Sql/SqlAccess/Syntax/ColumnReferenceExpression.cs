using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class ColumnReferenceExpression : ISqlElement
    {
        public string Name { get; set; }
        public IColumnSource Table { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitColumnReference(this);
        }

        public override string ToString()
        {
            return string.Format("{0}. Name: {1}, Table: {2}", typeof(ColumnReferenceExpression).Name, Name, Table);
        }
    }
}