namespace Simple1C.Impl.Sql.SchemaMapping
{
    internal class SingleLayout
    {
        public SingleLayout(string columnName, string nestedTableName)
        {
            ColumnName = columnName;
            NestedTableName = nestedTableName;
        }

        public string ColumnName { get; private set; }
        public string NestedTableName { get; private set; }
    }
}