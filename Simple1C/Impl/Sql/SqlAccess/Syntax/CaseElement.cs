namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class CaseElement
    {
        public ISqlElement Condition { get; set; }
        public ISqlElement Value { get; set; }
    }
}