using System;
using System.Collections.Generic;
using System.Data;

namespace Simple1C.Interface.Sql
{
    public interface IBatchWriter: IDisposable
    {
        void HandleNewDataSource(DataColumn[] newColumns);
        void RowsCache(List<object[]> data, int count);
    }
}