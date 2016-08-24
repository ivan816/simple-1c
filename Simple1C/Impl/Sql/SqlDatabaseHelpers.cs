using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Sql
{
    public static class SqlDatabaseHelpers
    {
        public static bool TableExists(this SqlDatabase sqlDatabase, string tableName)
        {
            return sqlDatabase.Exists("select * from sys.tables where name = @p0", tableName);
        }

        public static bool ColumnExists(this SqlDatabase sqlDatabase, string tableName, string columnName)
        {
            return sqlDatabase.Exists("select * from sys.columns where object_id = object_id(@p0) and name = @p1", tableName, columnName);
        }

        public static void TruncateTable(this SqlDatabase sqlDatabase, string tableName)
        {
            sqlDatabase.ExecuteNonQuery(string.Format("truncate table {0}", tableName));
        }

        public static void DropTable(this SqlDatabase sqlDatabase, string tableName)
        {
            sqlDatabase.ExecuteNonQuery(string.Format("drop table {0}", tableName));
        }

        public static int CalculateCount(this SqlDatabase sqlDatabase, string tableName)
        {
            return sqlDatabase.ExecuteInt(string.Format("select count(*) from {0}", tableName));
        }

        public static IEnumerable<string> GetAllTableNames(this SqlDatabase sqlDatabase)
        {
            return sqlDatabase.FirstColumnOf<string>("select name from sys.tables where type='U' order by name");
        }

        public static void Insert(this SqlDatabase sqlDatabase, string tableName, string fields, params object[] values)
        {
            sqlDatabase.ExecuteNonQuery(string.Format("insert into {0} ({1}) values ('{2}')", '[' + tableName + "]", fields,
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