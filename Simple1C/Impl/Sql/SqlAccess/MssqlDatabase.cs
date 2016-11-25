using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Simple1C.Impl.Sql.SqlAccess
{
    internal class MsSqlDatabase : AbstractSqlDatabase
    {
        public MsSqlDatabase(string connectionString, int commandTimeout = 100500)
            : base(connectionString, commandTimeout)
        {
        }

        public DataColumn[] GetColumns(string tableName)
        {
            return ExecuteWithResult(string.Format("select top 0 * from {0}", tableName),
                new object[0], c =>
                {
                    using (var reader = c.ExecuteReader())
                    {
                        var schemaTable = reader.GetSchemaTable();
                        if (schemaTable == null)
                            throw new InvalidOperationException("assertion failure");
                        var result = new DataColumn[schemaTable.Rows.Count];
                        for (var i = 0; i < result.Length; i++)
                        {
                            var r = schemaTable.Rows[i];
                            var type = (Type) r[SchemaTableColumn.DataType];
                            result[i] = new DataColumn
                            {
                                ColumnName = (string) r[SchemaTableColumn.ColumnName],
                                AllowDBNull = true,
                                DataType = type,
                                MaxLength = type == typeof(string) ? (int) r[SchemaTableColumn.ColumnSize] : -1
                            };
                        }
                        return result;
                    }
                });
        }

        public void BulkCopy(IDataReader dataReader, string tableName, int columnsCount)
        {
            using (var sqlBulkCopy = new SqlBulkCopy(ConnectionString))
            {
                sqlBulkCopy.ColumnMappings.Clear();
                for (var i = 0; i < columnsCount; i++)
                    sqlBulkCopy.ColumnMappings.Add(new SqlBulkCopyColumnMapping(i, i));
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

        public bool TableExists(string tableName)
        {
            return Exists("select * from sys.tables where name = @p0", tableName);
        }

        public bool ColumnExists(string tableName, string columnName)
        {
            return Exists("select * from sys.columns where object_id = object_id(@p0) and name = @p1", tableName,
                columnName);
        }

        protected override void AddParameter(DbCommand command, string name, object value)
        {
            ((SqlCommand) command).Parameters.AddWithValue(name, value);
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

            var fi = typeof(SqlBulkCopy).GetField("_sortedColumnMappings",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var sortedColumns = fi.GetValue(sqlBulkCopy);
            var items =
                (object[])
                    sortedColumns.GetType()
                        .GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(sortedColumns);

            var itemdata = items[index].GetType().GetField("_metadata", BindingFlags.NonPublic | BindingFlags.Instance);
            var metadata = itemdata.GetValue(items[index]);

            var column =
                metadata.GetType()
                    .GetField("column", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(metadata);
            var length =
                metadata.GetType()
                    .GetField("length", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(metadata);
            throw new InvalidOperationException(
                string.Format("Column: {0} contains data with a length greater than: {1}", column, length), ex);
        }

        protected override DbConnection CreateConnection()
        {
            return new SqlConnection();
        }

        protected override DbCommand CreateCommand()
        {
            return new SqlCommand();
        }

        protected override string GetSqlType(DataColumn column)
        {
            var type = column.DataType;
            if (type == typeof(bool))
                return "bit";
            if (type == typeof(string))
                return "varchar(" + (column.MaxLength > 0 ? column.MaxLength : 1000) + ")";
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
            if (type == typeof(byte[]))
                return "image";
            throw new InvalidOperationException(string.Format("unsupported type [{0}]", type.Name));
        }
    }
}