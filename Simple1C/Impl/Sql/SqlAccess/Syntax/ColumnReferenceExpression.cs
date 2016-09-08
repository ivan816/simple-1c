namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class ColumnReferenceExpression : ISqlElement
    {
        public string Name { get; set; }
        public string TableName { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitColumnReference(this);
        }
    }
}