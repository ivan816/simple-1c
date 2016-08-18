namespace Simple1C.Impl.Sql
{
    public class MappingSchema
    {
        public TableMapping[] Tables { get; set; }

        public static MappingSchema Parse(string source)
        {
            return new MappingSchema();
        }
    }
}