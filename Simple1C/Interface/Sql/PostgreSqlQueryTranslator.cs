using System;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Interface.Sql
{
    public class PostgreSqlQueryTranslator
    {
        private readonly PostgreeSqlSchemaStore mappingSchema;

        public PostgreSqlQueryTranslator(string connectionString, int commandTimeout = 100500)
        {
            mappingSchema = new PostgreeSqlSchemaStore(new PostgreeSqlDatabase(connectionString, commandTimeout));
        }

        public string Transale(string query1CText)
        {
            return Transale(query1CText, new int[0]);
        }

        public string Transale(string query1CText, int[] areas)
        {
            var translator = new QueryToSqlTranslator(mappingSchema, areas)
            {
                CurrentDate = DateTime.Now
            };
            return translator.Translate(query1CText);
        }
    }
}