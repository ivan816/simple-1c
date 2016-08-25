using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using Npgsql;

namespace Simple1C.Impl.Sql.SqlAccess
{
    public class PostgreeSqlDatabase : AbstractSqlDatabase
    {
        public PostgreeSqlDatabase(string connectionString, int commandTimeout = 100500)
            : base(connectionString, commandTimeout)
        {
        }

        protected override void AddParameter(DbCommand command, string name, object value)
        {
            ((NpgsqlCommand)command).Parameters.AddWithValue(name, value);
        }

        public override void BulkCopy(DataTable dataTable)
        {
            using (var npgsqlConnection = new NpgsqlConnection(ConnectionString))
            using (var writer = npgsqlConnection.BeginBinaryImport(GetCopyFromCommandText(dataTable)))
                foreach (var r in dataTable.Rows)
                    writer.WriteRow(r);
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
                return "varchar(" + (column.MaxLength > 0 ? column.MaxLength : 1000) + ")";
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

        private static string GetCopyFromCommandText(DataTable dataTable)
        {
            var b = new StringBuilder();
            b.Append("COPY data (");
            var isFirst = true;
            foreach (var column in dataTable.Columns.Cast<DataColumn>())
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