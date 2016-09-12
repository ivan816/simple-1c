namespace Simple1C.Impl.Sql.SchemaMapping
{
    internal interface IMappingSource
    {
        TableMapping ResolveTable(string queryName);
    }
}