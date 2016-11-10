using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Simple1C.Impl.Sql;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess;

namespace Simple1C.Interface.Sql
{
    public class QueryExecutor
    {
        private readonly SourceDescriptor[] sources;

        public QueryExecutor(QuerySource[] sources)
        {
            this.sources = sources.Select(x =>
            {
                var db = new PostgreeSqlDatabase(x.postgreSqlConnectionString);
                return new SourceDescriptor
                {
                    db = db,
                    areas = x.areas,
                    translator = new PostgreSqlQueryTranslator(new PostgreeSqlSchemaStore(db))
                };
            })
                .ToArray();
        }

        public void ExecuteParallel(string queryText, IBatchWriter writer, CancellationToken token, QueryExecutionOptions options = null)
        {
            var batchSize = options == null || options.BatchSize == 0 ? 1024 : options.BatchSize;
            var parallelOptions = options == null || options.ParallelOptions == null
                ? new ParallelOptions {MaxDegreeOfParallelism = sources.Length}
                : options.ParallelOptions;
            parallelOptions.CancellationToken = token;
            using (var batchWriter = new BatchWriter(writer, batchSize))
            {
                var w = batchWriter;
                try
                {
                    Parallel.ForEach(sources, parallelOptions, (source, state) =>
                    {
                        try
                        {
                            if (state.ShouldExitCurrentIteration)
                                return;
                            var sql = source.translator.Transale(queryText, source.areas);
                            if (state.ShouldExitCurrentIteration)
                                return;
                            source.db.Execute(sql, new object[0], c =>
                            {
                                if (state.ShouldExitCurrentIteration)
                                    return;
                                using (var reader = c.ExecuteReader())
                                {
                                    if (state.ShouldExitCurrentIteration)
                                        return;
                                    w.HandleNewDataSource(reader);
                                    if (state.ShouldExitCurrentIteration)
                                        return;
                                    while (reader.Read())
                                    {
                                        if (state.ShouldExitCurrentIteration)
                                            return;
                                        w.InsertRow(reader);
                                    }
                                }
                            });
                        }
                        catch (Exception e)
                        {
                            const string messageFormat = "error has occurred for database [{0}]";
                            throw new InvalidOperationException(
                                string.Format(messageFormat, source.db.ConnectionString), e);
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private class SourceDescriptor
        {
            public PostgreeSqlDatabase db;
            public int[] areas;
            public PostgreSqlQueryTranslator translator;
        }
    }
}