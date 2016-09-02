namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class EqCondition
    {
        public string ColumnTableName { get; set; }
        public string ColumnName { get; set; }
        public string ComparandConstantValue { get; set; }
        public string ComparandTableName { get; set; }
        public string ComparandColumnName { get; set; }
    }
}