namespace Simple1C.Impl.Sql
{
    internal interface ITableMappingSource
    {
        TableMapping GetByQueryName(string queryName);
    }
}