using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Npgsql;

namespace Simple1C.Impl.Sql.SqlAccess
{
    internal class PostgreeSqlDatabase : AbstractSqlDatabase
    {
        public PostgreeSqlDatabase(string connectionString, int commandTimeout = 100500)
            : base(connectionString, commandTimeout)
        {
        }

        public bool TableExists(string tableName)
        {
            const string sqlFormat = @"SELECT cast(to_regclass('public.{0}') as varchar(200));";
            return !string.IsNullOrEmpty(ExecuteString(string.Format(sqlFormat, tableName)));
        }

        protected override void AddParameter(DbCommand command, string name, object value)
        {
            ((NpgsqlCommand) command).Parameters.AddWithValue(name, value);
        }

        public void BulkCopy(string tableName, DataColumn[] columns, IEnumerable<object[]> data)
        {
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                var copyCommandText = GetCopyCommandText(tableName, columns);
                using (var writer = connection.BeginBinaryImport(copyCommandText))
                    foreach (var r in data)
                        writer.WriteRow(r);
            }
        }

        protected override DbConnection CreateConnection()
        {
            return new NpgsqlConnection();
        }

        protected override DbCommand CreateCommand()
        {
            return new NpgsqlCommand {AllResultTypesAreUnknown = true};
        }

        protected override string GetSqlType(DataColumn column)
        {
            var type = column.DataType;
            if (type == typeof(bool))
                return "boolean";
            if (type == typeof(string))
                return column.MaxLength < 0
                    ? "text"
                    : "varchar(" + column.MaxLength + ")";
            if (type == typeof(DateTime))
                return "date";
            if (type == typeof(decimal))
                return "decimal(29,2)";
            if (type == typeof(int))
                return "int";
            if (type == typeof(long))
                return "bigint";
            if (type == typeof(Guid))
                return "uuid";
            throw new InvalidOperationException(string.Format("unsupported type [{0}]", type.Name));
        }

        private static string GetCopyCommandText(string tableName, DataColumn[] columns)
        {
            var b = new StringBuilder();
            b.Append("COPY ");
            b.Append(tableName);
            b.Append(" (");
            var isFirst = true;
            foreach (var column in columns)
            {
                if (isFirst)
                    isFirst = false;
                else
                    b.Append(", ");
                b.Append(column.ColumnName);
            }
            b.Append(") FROM STDIN (FORMAT BINARY)");
            return b.ToString();
        }
    }
}