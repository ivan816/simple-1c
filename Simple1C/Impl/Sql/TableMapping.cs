namespace Simple1C.Impl.Sql
{
    public class TableMapping
    {
        public string QueryName { get; set; }
        public string DbName { get; set; }
        public ColumnMapping[] Columns { get; set; }
    }
}