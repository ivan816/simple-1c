using System.Data;

namespace Simple1C.Interface.Sql
{
    public interface IQuery1CReader
    {
        DataColumn[] Columns { get; }
        bool Read();
        object[] GetValues();
    }
}