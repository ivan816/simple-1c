using System.Collections.Generic;
using System.Data;
using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Interface.Sql;

namespace Simple1C.Impl.Sql
{
    internal class MsSqlBulkCopyWriter : IWriter
    {
        private readonly List<object[]> rows = new List<object[]>();
        private int filledRowsCount;
        private readonly MsSqlDatabase target;
        private readonly string tableName;
        private readonly bool historyMode;
        private readonly int batchSize;
        private DataColumn[] columns;

        public MsSqlBulkCopyWriter(MsSqlDatabase target, string tableName,
            bool historyMode, int batchSize)
        {
            this.target = target;
            this.tableName = tableName;
            this.historyMode = historyMode;
            this.batchSize = batchSize;
        }

        public void BeginWrite(DataColumn[] newColumns)
        {
            columns = newColumns;
            if (historyMode)
            {
                if (!target.TableExists(tableName))
                    target.CreateTable(tableName, newColumns);
                else
                {
                    var existingColumns = target.GetColumns(tableName);
                    DatabaseHelpers.CheckColumnsAreEqual(existingColumns, "existing", newColumns, "new");
                }
            }
            else
            {
                if (target.TableExists(tableName))
                    target.DropTable("dbo." + tableName);
                target.CreateTable(tableName, newColumns);
            }
        }

        public void Write(RowAccessor row)
        {
            var currentRowIndex = filledRowsCount;
            object[] rowData;
            if (currentRowIndex < rows.Count)
                rowData = rows[currentRowIndex];
            else
                rows.Add(rowData = new object[columns.Length]);
            row.GetValues(rowData);
            filledRowsCount++;
            if (filledRowsCount == batchSize)
                Flush();
        }

        public void EndWrite()
        {
            if (filledRowsCount > 0)
                Flush();
        }

        private void Flush()
        {
            var reader = new InMemoryDataReader(rows, filledRowsCount, columns.Length);
            target.BulkCopy(reader, tableName, columns.Length);
            filledRowsCount = 0;
        }
    }
}