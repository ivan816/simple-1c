namespace Simple1C.Impl.Sql.SchemaMapping
{
    internal interface IMappingSource
    {
        TableMapping ResolveTableOrNull(string queryName);
    }
}