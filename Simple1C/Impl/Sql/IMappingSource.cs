namespace Simple1C.Impl.Sql
{
    internal interface IMappingSource
    {
        TableMapping ResolveTable(string queryName);
    }
}