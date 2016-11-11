using System;
using System.Threading.Tasks;
using Npgsql;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Impl.Sql.Translation;
using Simple1C.Interface.Sql;

namespace Simple1C
{
    public static class Sql
    {
        public static string Translate(QuerySource source, string queryText)
        {
            return Translate(source, queryText, DateTime.Now);
        }

        public static void Execute(QuerySource[] sources, string queryText, IWriter writer,
            ParallelOptions options = null)
        {
            options = options ?? new ParallelOptions {MaxDegreeOfParallelism = sources.Length};
            Execute(sources, queryText, writer, options, false);
        }

        internal static void Execute(QuerySource[] sources, string queryText, IWriter writer,
            ParallelOptions options, bool dumpSql)
        {
            RowAccessor rowAccessor = null;
            var locker = new object();
            var runTimestamp = DateTime.Now;
            var writeStarted = false;
            try
            {
                Parallel.ForEach(sources, options, (source, state) =>
                {
                    try
                    {
                        if (state.ShouldExitCurrentIteration)
                            return;
                        var sql = Translate(source, queryText, runTimestamp);
                        if (dumpSql)
                            Console.Out.WriteLine("\r\n[{0}]\r\n{1}\r\n====>\r\n{2}",
                                source.ConnectionString, queryText, sql);
                        if (state.ShouldExitCurrentIteration)
                            return;
                        var db = new PostgreeSqlDatabase(source.ConnectionString);
                        db.Execute(sql, new object[0], c =>
                        {
                            if (state.ShouldExitCurrentIteration)
                                return;
                            using (var reader = (NpgsqlDataReader) c.ExecuteReader())
                            {
                                if (state.ShouldExitCurrentIteration)
                                    return;
                                var columns = DatabaseHelpers.GetColumns(reader);
                                lock (locker)
                                    if (rowAccessor == null)
                                    {
                                        rowAccessor = new RowAccessor(columns);
                                        writer.BeginWrite(columns);
                                        writeStarted = true;
                                    }
                                    else
                                        DatabaseHelpers.CheckColumnsAreEqual(rowAccessor.Columns, "original", columns, "current");
                                if (state.ShouldExitCurrentIteration)
                                    return;
                                while (reader.Read())
                                {
                                    if (state.ShouldExitCurrentIteration)
                                        return;
                                    lock (locker)
                                    {
                                        rowAccessor.Reader = reader;
                                        writer.Write(rowAccessor);
                                    }
                                }
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        const string messageFormat = "error has occurred for database [{0}]";
                        throw new InvalidOperationException(
                            string.Format(messageFormat, source.ConnectionString), e);
                    }
                });
            }
            finally
            {
                if (writeStarted)
                    writer.EndWrite();
            }
        }

        private static string Translate(QuerySource source, string queryText, DateTime runTimestamp)
        {
            var db = new PostgreeSqlDatabase(source.ConnectionString);
            var schemeStore = new PostgreeSqlSchemaStore(db);
            var translator = new QueryToSqlTranslator(schemeStore, source.Areas)
            {
                CurrentDate = runTimestamp
            };
            return translator.Translate(queryText);
        }
    }
}