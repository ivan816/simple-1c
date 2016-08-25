namespace Simple1C.Impl.Sql
{
    public interface ITableMappingSource
    {
        TableMapping GetByQueryName(string queryName);
    }
}