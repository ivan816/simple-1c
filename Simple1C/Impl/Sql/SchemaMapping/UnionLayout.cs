namespace Simple1C.Impl.Sql.SchemaMapping
{
    internal class UnionLayout
    {
        public UnionLayout(string typeColumnName, string tableIndexColumnName,
            string referenceColumnName, string[] nestedTables)
        {
            TypeColumnName = typeColumnName;
            TableIndexColumnName = tableIndexColumnName;
            ReferenceColumnName = referenceColumnName;
            NestedTables = nestedTables;
        }

        public string TypeColumnName { get; private set; }
        public string TableIndexColumnName { get; private set; }
        public string ReferenceColumnName { get; private set; }
        public string[] NestedTables { get; private set; }
    }
}