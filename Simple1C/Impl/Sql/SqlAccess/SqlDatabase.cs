using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Simple1C.Impl.Sql.SqlAccess
{
    public abstract class AbstractSqlDatabase
    {
        private readonly int commandTimeout;
        protected readonly string connectionString;

        protected AbstractSqlDatabase(string connectionString, int commandTimeout = 100500)
        {
            this.commandTimeout = commandTimeout;
            this.connectionString = connectionString;
        }

        public int ExecuteInt(string commandText)
        {
            return ExecuteScalar<int>(commandText);
        }

        public long ExecuteLong(string commandText)
        {
            return ExecuteScalar<long>(commandText);
        }

        public bool ExecuteBool(string commandText)
        {
            return ExecuteScalar<bool>(commandText);
        }

        public decimal ExecuteDecimal(string commandText)
        {
            return ExecuteScalar<decimal>(commandText);
        }

        public string ExecuteString(string commandText)
        {
            return ExecuteScalar<string>(commandText);
        }

        private TResult ExecuteScalar<TResult>(string commandText, params object[] args)
        {
            return Execute(commandText, args, c => (TResult)Convert.ChangeType(c.ExecuteScalar(), typeof(TResult)));
        }

        public bool Exists(string sql, params object[] args)
        {
            return Execute(sql, args, c =>
            {
                using (var reader = c.ExecuteReader())
                    return reader.Read();
            });
        }

        public void TruncateTable(string tableName)
        {
            ExecuteNonQuery(string.Format("truncate table {0}", tableName));
        }

        public void DropTable(string tableName)
        {
            ExecuteNonQuery(string.Format("drop table {0}", tableName));
        }

        public IEnumerable<T> ExecuteReader<T>(string commandText, object[] args, Func<DbDataReader, T> map)
        {
            return Execute(commandText, args, c =>
            {
                using (var reader = c.ExecuteReader())
                    return ReadAll(reader, map).ToArray();
            });
        }

        private static IEnumerable<T> ReadAll<T>(DbDataReader reader, Func<DbDataReader, T> map)
        {
            while (reader.Read())
                yield return map(reader);
        }

        public void CreateTable(DataTable dataTable)
        {
            var sqlBuilder = new StringBuilder();
            sqlBuilder.Append("CREATE TABLE ");
            sqlBuilder.Append(dataTable.TableName);
            sqlBuilder.AppendLine();
            sqlBuilder.Append("(");
            for (var i = 0; i < dataTable.Columns.Count; i++)
            {
                sqlBuilder.AppendLine();
                sqlBuilder.Append('\t');
                var column = dataTable.Columns[i];
                sqlBuilder.Append(column.ColumnName);
                sqlBuilder.Append(' ');
                sqlBuilder.Append(GetSqlType(column));
                sqlBuilder.Append(column.AllowDBNull ? " NULL" : " NOT NULL");
                if (i != dataTable.Columns.Count - 1)
                    sqlBuilder.Append(',');
                sqlBuilder.AppendLine();
            }
            sqlBuilder.Append(")");
            ExecuteNonQuery(sqlBuilder.ToString());
        }

        public int ExecuteNonQuery(string commandText, params object[] parameters)
        {
            return Execute(commandText, parameters, c => c.ExecuteNonQuery());
        }

        public TResult Execute<TResult>(string commandText, object[] parameters, Func<DbCommand, TResult> useCommand)
        {
            using (var connection = CreateConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();
                using (var command = CreateCommand())
                {
                    for (var i = 0; i < parameters.Length; i++)
                        AddParameter(command, "@p" + i, parameters[i]);
                    command.CommandText = commandText;
                    command.CommandTimeout = commandTimeout;
                    command.Connection = connection;
                    return useCommand(command);
                }
            }
        }

        protected abstract DbConnection CreateConnection();
        protected abstract DbCommand CreateCommand();
        protected abstract string GetSqlType(DataColumn column);
        protected abstract void AddParameter(DbCommand command, string name, object value);
        public abstract void BulkCopy(DataTable dataTable);
    }
}