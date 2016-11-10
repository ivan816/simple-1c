using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Interface.Sql;

namespace Simple1C.Impl.Sql
{
    internal class MsSqlBatchWriter : IBatchWriter
    {
        private readonly MsSqlDatabase target;
        private readonly bool historyMode;
        private DataColumn[] columns;
        private readonly string tableName;

        public MsSqlBatchWriter(MsSqlDatabase target, string tableName, bool historyMode)
        {
            this.target = target;
            this.historyMode = historyMode;
            this.tableName = tableName;
        }

        public void HandleNewDataSource(DataColumn[] newColumns)
        {
            if (columns != null)
            {
                CheckColumns(newColumns, "original", newColumns, "current");
                return;
            }
            columns = newColumns;
            if (historyMode)
            {
                if (!target.TableExists(tableName))
                    target.CreateTable(tableName, newColumns);
                else
                {
                    var existingColumns = target.GetColumns(tableName);
                    CheckColumns(existingColumns, "existing", newColumns, "new");
                }
            }
            else
            {
                if (target.TableExists(tableName))
                    target.DropTable("dbo." + tableName);
                target.CreateTable(tableName, newColumns);
            }
        }

        private static void CheckColumns(DataColumn[] a, string aName, DataColumn[] b, string bName)
        {
            var aFormat = FormatColumns(a);
            var bFormat = FormatColumns(b);
            if (aFormat == bFormat)
                return;
            const string messageFormat = "inconsistent columns {0}(first) and {1}(second):\r\n{2}\r\n{3}\r\n";
            throw new InvalidOperationException(string.Format(messageFormat,
                aName, bName, aFormat, bFormat));
        }

        private static string FormatColumns(DataColumn[] c)
        {
            return c.Select(FormatColumn).JoinStrings(",");
        }

        private static string FormatColumn(DataColumn c)
        {
            var lengthSpec = "";
            if (c.DataType == typeof(string))
            {
                var maxLength = c.MaxLength == -1 ? 1000 : c.MaxLength;
                lengthSpec = "[" + maxLength + "]";
            }
            return string.Format("{0}:{1}{2}", c.ColumnName, c.DataType.FormatName(), lengthSpec);
        }

        public void RowsCache(List<object[]> data, int count)
        {
            var reader = new InMemoryDataReader(data, count, columns.Length);
            target.BulkCopy(reader, tableName, columns.Length);
        }

        public void Dispose()
        {
        }
    }
}