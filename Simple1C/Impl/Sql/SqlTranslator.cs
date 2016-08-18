namespace Simple1C.Impl.Sql
{
    public class SqlTranslator
    {
        private readonly MappingSchema mappingSchema;

        public SqlTranslator(MappingSchema mappingSchema)
        {
            this.mappingSchema = mappingSchema;
        }

        public string Translate(string source)
        {
            return source;
        }
    }
}