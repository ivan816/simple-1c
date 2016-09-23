namespace Simple1C.Impl.Sql.SchemaMapping
{
    internal class SingleLayout
    {
        public SingleLayout(string dbColumnName, string nestedTableName)
        {
            DbColumnName = dbColumnName;
            NestedTableName = nestedTableName;
        }

        public string DbColumnName { get; private set; }
        public string NestedTableName { get; private set; }
    }
}