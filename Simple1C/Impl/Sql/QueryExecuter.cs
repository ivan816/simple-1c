using System;
using System.Diagnostics;
using System.Threading;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql
{
    internal class QueryExecuter
    {
        private readonly QuerySource[] sources;
        private readonly MsSqlDatabase target;
        private readonly bool dumpSql;
        private readonly bool historyMode;
        private volatile bool errorOccured;
        private readonly string queryText;
        private readonly string targetTableName;

        public QueryExecuter(QuerySource[] sources, MsSqlDatabase target, string queryText,
            string targetTableName, bool dumpSql, bool historyMode)
        {
            this.sources = sources;
            this.target = target;
            this.queryText = queryText;
            this.targetTableName = targetTableName;
            this.dumpSql = dumpSql;
            this.historyMode = historyMode;
        }

        public bool Execute()
        {
            var s = Stopwatch.StartNew();
            var sourceThreads = new Thread[sources.Length];
            using (var writer = new BatchWriter(target, targetTableName, 1000, historyMode))
            {
                var w = writer;
                for (var i = 0; i < sourceThreads.Length; i++)
                {
                    var source = sources[i];
                    sourceThreads[i] = new Thread(delegate(object _)
                    {
                        try
                        {
                            var mappingSchema = new PostgreeSqlSchemaStore(source.db);
                            var translator = new QueryToSqlTranslator(mappingSchema, source.areas);
                            var sql = translator.Translate(queryText);
                            if (dumpSql)
                                Console.Out.WriteLine("\r\n[{0}]\r\n{1}\r\n====>\r\n{2}",
                                    source.db.ConnectionString, queryText, sql);
                            source.db.Execute(sql, new object[0], c =>
                            {
                                using (var reader = c.ExecuteReader())
                                {
                                    w.EnsureTable(reader);
                                    while (reader.Read())
                                    {
                                        if (errorOccured)
                                            throw new OperationCanceledException();
                                        w.InsertRow(reader);
                                    }
                                }
                            });
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (Exception e)
                        {
                            errorOccured = true;
                            Console.Out.WriteLine("error for [{0}]\r\n{1}", source.db.ConnectionString, e);
                        }
                    });
                    sourceThreads[i].Start();
                }
                foreach (var t in sourceThreads)
                    t.Join();
            }
            s.Stop();
            Console.Out.WriteLine("\r\ndone, [{0}] millis", s.ElapsedMilliseconds);
            return !errorOccured;
        }
    }
}