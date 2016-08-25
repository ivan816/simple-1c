using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Simple1C.Impl.Sql.SqlAccess
{
    public class MssqlDatabase
    {
        private readonly int commandTimeout;
        private readonly string connectionString;

        public MssqlDatabase(string connectionString, int commandTimeout = 100500)
        {
            this.commandTimeout = commandTimeout;
            this.connectionString = connectionString;
        }

        public bool Exists(string sql, params object[] args)
        {
            return Execute(sql, args, c =>
            {
                using (var reader = c.ExecuteReader())
                    return reader.Read();
            });
        }

        public int ExecuteInt(string commandText, params object[] args)
        {
            return ExecuteScalar<int>(commandText, args);
        }

        public long ExecuteLong(string commandText, params object[] args)
        {
            return ExecuteScalar<long>(commandText, args);
        }

        public bool ExecuteBool(string commandText, params object[] args)
        {
            return ExecuteScalar<bool>(commandText, args);
        }

        public decimal ExecuteDecimal(string commandText, params object[] args)
        {
            return ExecuteScalar<decimal>(commandText, args);
        }

        public string ExecuteString(string commandText, params object[] args)
        {
            return ExecuteScalar<string>(commandText, args);
        }

        private TResult ExecuteScalar<TResult>(string commandText, params object[] args)
        {
            return Execute(commandText, args, c => (TResult)Convert.ChangeType(c.ExecuteScalar(), typeof(TResult)));
        }

        public IEnumerable<T> ExecuteReader<T>(string commandText, object[] args, Func<SqlDataReader, T> map)
        {
            return Execute(commandText, args, c =>
            {
                using (var reader = c.ExecuteReader())
                    return ReadAll(reader, map).ToArray();
            });
        }

        public T ExecuteReaderSingle<T>(string commandText, object[] args, Func<SqlDataReader, T> action)
        {
            return Execute(commandText, args, c => action(c.ExecuteReader()));
        }

        private static IEnumerable<T> ReadAll<T>(SqlDataReader reader, Func<SqlDataReader, T> map)
        {
            while (reader.Read())
                yield return map(reader);
        }

        public void BulkCopy(DataTable dataTable, IEnumerable<string> columns)
        {
            using (var sqlBulkCopy = new SqlBulkCopy(connectionString))
            {
                sqlBulkCopy.ColumnMappings.Clear();
                foreach (var column in columns)
                    sqlBulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(column, column));
                sqlBulkCopy.DestinationTableName = dataTable.TableName;
                try
                {
                    sqlBulkCopy.WriteToServer(dataTable);
                }
                catch (SqlException ex)
                {
                    if (IsInvalidColumnLength(ex))
                        RethrowWithColumnName(ex, sqlBulkCopy);
                    else
                        throw;
                }
            }
        }

        public void BulkCopy(IDataReader dataReader, string tableName, SqlBulkCopyColumnMapping[] columnMappings)
        {
            using (var sqlBulkCopy = new SqlBulkCopy(connectionString))
            {
                sqlBulkCopy.ColumnMappings.Clear();
                foreach (var t in columnMappings)
                    sqlBulkCopy.ColumnMappings.Add(t);
                sqlBulkCopy.DestinationTableName = tableName;
                try
                {
                    sqlBulkCopy.WriteToServer(dataReader);
                }
                catch (SqlException ex)
                {
                    if (IsInvalidColumnLength(ex))
                        RethrowWithColumnName(ex, sqlBulkCopy);
                    else
                        throw;
                }
            }
        }

        public IDictionary<string, int> GetColumnNameToIndexMap(string tableName)
        {
            var result = new DataTable();
            using (var adapter = new SqlDataAdapter(string.Format("select top 0 * from {0}", tableName), connectionString))
                adapter.FillSchema(result, SchemaType.Source);
            return result.Columns.Cast<DataColumn>().ToDictionary(x => x.ColumnName, x => x.Ordinal);
        }

        private static bool IsInvalidColumnLength(SqlException ex)
        {
            return ex.Message.Contains("Received an invalid column length from the bcp client for colid");
        }

        private static void RethrowWithColumnName(SqlException ex, SqlBulkCopy sqlBulkCopy)
        {
            const string pattern = @"\d+";
            var match = Regex.Match(ex.Message, pattern);
            var index = Convert.ToInt32(match.Value) - 1;

            var fi = typeof(SqlBulkCopy).GetField("_sortedColumnMappings", BindingFlags.NonPublic | BindingFlags.Instance);
            var sortedColumns = fi.GetValue(sqlBulkCopy);
            var items = (Object[])sortedColumns.GetType().GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sortedColumns);

            var itemdata = items[index].GetType().GetField("_metadata", BindingFlags.NonPublic | BindingFlags.Instance);
            var metadata = itemdata.GetValue(items[index]);

            var column = metadata.GetType().GetField("column", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(metadata);
            var length = metadata.GetType().GetField("length", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(metadata);
            throw new InvalidOperationException(string.Format("Column: {0} contains data with a length greater than: {1}", column, length), ex);
        }

        public void BulkCopy(DataTable dataTable)
        {
            BulkCopy(dataTable, dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName));
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

        private static string GetSqlType(DataColumn column)
        {
            var type = column.DataType;
            if (type == typeof(bool))
                return "bit";
            if (type == typeof(string))
                return "nvarchar(" + (column.MaxLength > 0 ? column.MaxLength : 1000) + ")";
            if (type == typeof(DateTime))
                return "datetime";
            if (type == typeof(decimal))
                return "decimal(29,2)";
            if (type == typeof(int))
                return "int";
            if (type == typeof(long))
                return "bigint";
            if (type == typeof(Guid))
                return "uniqueidentifier";
            throw new InvalidOperationException(string.Format("unsupported type [{0}]", type.Name));
        }

        public DataTable GetSchema(string tableName)
        {
            var result = new DataTable();
            using (var adapter = new SqlDataAdapter(string.Format("select top 0 * from {0}", tableName), connectionString))
                adapter.FillSchema(result, SchemaType.Source);
            return result;
        }

        public IEnumerable<T> FirstColumnOf<T>(string sql, params object[] args)
        {
            return ExecuteReader(sql, args, r => (T)r[0]);
        }

        public int ExecuteNonQuery(string commandText, params object[] args)
        {
            return Execute(commandText, args, c => c.ExecuteNonQuery());
        }

        public void ExecuteDataAdapter(string commandText, object[] args, Action<SqlDataAdapter> useAdapter)
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                var selectCommandText = CreateSqlCommand(commandText, args);
                selectCommandText.Connection = sqlConnection;
                useAdapter(new SqlDataAdapter(selectCommandText));
            }
        }

        public bool ViewExists(string viewName)
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                var schema = sqlConnection.GetSchema("Tables");
                return schema.Rows.Cast<DataRow>().Any(r => (string)r[2] == viewName && (string)r[3] == "VIEW");
            }
        }

        private SqlCommand CreateSqlCommand(string commandText, object[] parameters)
        {
            var command = new SqlCommand(commandText);
            for (int i = 0; i < parameters.Length; i++)
                command.Parameters.AddWithValue("@p" + i, parameters[i]);
            command.CommandTimeout = commandTimeout;
            return command;
        }

        private TResult Execute<TResult>(string commandText, object[] parameters, Func<SqlCommand, TResult> useCommand)
        {
            using (var sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                using (var command = CreateSqlCommand(commandText, parameters))
                {
                    command.Connection = sqlConnection;
                    return useCommand(command);
                }
            }
        }
    }
}