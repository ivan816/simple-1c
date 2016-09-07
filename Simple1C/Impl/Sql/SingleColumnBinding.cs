namespace Simple1C.Impl.Sql
{
    internal class SingleColumnBinding
    {
        public SingleColumnBinding(string columnName, string nestedTableName)
        {
            ColumnName = columnName;
            NestedTableName = nestedTableName;
        }

        public string ColumnName { get; private set; }
        public string NestedTableName { get; private set; }
    }
}