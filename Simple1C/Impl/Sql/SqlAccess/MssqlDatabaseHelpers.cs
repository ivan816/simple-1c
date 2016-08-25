using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Sql.SqlAccess
{
    public static class MssqlDatabaseHelpers
    {
        public static bool TableExists(this MssqlDatabase mssqlDatabase, string tableName)
        {
            return mssqlDatabase.Exists("select * from sys.tables where name = @p0", tableName);
        }

        public static bool ColumnExists(this MssqlDatabase mssqlDatabase, string tableName, string columnName)
        {
            return mssqlDatabase.Exists("select * from sys.columns where object_id = object_id(@p0) and name = @p1", tableName, columnName);
        }

        public static void TruncateTable(this MssqlDatabase mssqlDatabase, string tableName)
        {
            mssqlDatabase.ExecuteNonQuery(string.Format("truncate table {0}", tableName));
        }

        public static void DropTable(this MssqlDatabase mssqlDatabase, string tableName)
        {
            mssqlDatabase.ExecuteNonQuery(string.Format("drop table {0}", tableName));
        }

        public static int CalculateCount(this MssqlDatabase mssqlDatabase, string tableName)
        {
            return mssqlDatabase.ExecuteInt(string.Format("select count(*) from {0}", tableName));
        }

        public static IEnumerable<string> GetAllTableNames(this MssqlDatabase mssqlDatabase)
        {
            return mssqlDatabase.FirstColumnOf<string>("select name from sys.tables where type='U' order by name");
        }

        public static void Insert(this MssqlDatabase mssqlDatabase, string tableName, string fields, params object[] values)
        {
            mssqlDatabase.ExecuteNonQuery(string.Format("insert into {0} ({1}) values ('{2}')", '[' + tableName + "]", fields,
                FormatValues(values.AsEnumerable())));
        }

        private static string FormatValues(IEnumerable<object> values)
        {
            return values.Select(FormatValue).JoinStrings(", ");
        }

        private static string FormatValue(object value)
        {
            var result = value.ToString();
            return value is Guid || value is string ? "'" + result + "'" : result;
        }
    }
}