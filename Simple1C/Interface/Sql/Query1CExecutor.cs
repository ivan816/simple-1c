using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Npgsql;
using Simple1C.Impl.Sql;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Interface.Sql
{
    public class Query1CExecutor
    {
        private readonly QuerySource[] sources;

        public Query1CExecutor(IEnumerable<QuerySourceInfo> infos)
        {
            sources = infos.Select(x => new QuerySource
            {
                db = new PostgreeSqlDatabase(x.ConnectionString),
                areas = x.Areas
            }).ToArray();
        }

        public IQuery1CReader Execute(string query, object[] parameters = null)
        {
            if (parameters == null)
                parameters = new object[] {};
            var queryReader = new Query1CReader();
            var contexts = sources.Select(source =>
                new ExecutionContext
                {
                    thread = new Thread(ExecuteQueryForConnection),
                    source = source,
                    query = query,
                    parameters = parameters,
                    queryReader = queryReader
                }).ToArray();
            queryReader.ExecutionThreads = contexts.Select(x => x.thread).ToArray();
            foreach (var ctx  in contexts)
                ctx.thread.Start(ctx);
        
            return queryReader;
        }

        private static void ExecuteQueryForConnection(object context)
        {
            var ctx = (ExecutionContext) context;
            try
            {
                var mappingSchema = new PostgreeSqlSchemaStore(ctx.source.db);
                var translator = new QueryToSqlTranslator(mappingSchema, ctx.source.areas)
                {
                    CurrentDate = DateTime.Now
                };
                var sql = translator.Translate(ctx.query);
                ctx.source.db.Execute(sql, ctx.parameters, c =>
                {
                    using (var reader = c.ExecuteReader())
                    {
                        var columns = PostgreeSqlDatabase.GetColumns((NpgsqlDataReader) reader);
                        ctx.queryReader.Columns = columns;
                        while (reader.Read())
                        {
                            if (ctx.queryReader.Errors.Count > 0)
                                throw new OperationCanceledException();
                            var vals = new object[columns.Length];
                            reader.GetValues(vals);
                            ctx.queryReader.Queue.Enqueue(vals);
                        }
                    }
                });
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                const string msg = "Execution error in the db: [{0}]";
                ctx.queryReader.Errors.Add(new InvalidOperationException(string.Format(msg,
                    ctx.source.db.ConnectionString), e));
            }
        }

        private class ExecutionContext
        {
            public object[] parameters;
            public string query;
            public Query1CReader queryReader;
            public QuerySource source;
            public Thread thread;
        }
    }
}