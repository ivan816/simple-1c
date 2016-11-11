using System.Data;

namespace Simple1C.Interface.Sql
{
    public interface IWriter
    {
        void BeginWrite(DataColumn[] columns);
        void Write(RowAccessor row);
        void EndWrite();
    }
}