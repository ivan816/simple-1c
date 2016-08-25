using System;
using System.Collections.Generic;
using System.Data;

namespace Simple1C.Impl.Sql.SqlAccess
{
    internal class InMemoryDataReader : IDataReader
    {
        private readonly List<object[]> data;
        private readonly int rowCount;
        private readonly int fieldCount;
        private object[] current;
        private int currentRowIndex = -1;

        public InMemoryDataReader(List<object[]> data, int rowCount, int fieldCount)
        {
            this.data = data;
            this.rowCount = rowCount;
            this.fieldCount = fieldCount;
        }

        public void Dispose()
        {
            throw new NotSupportedException();
        }

        public string GetName(int i)
        {
            throw new NotSupportedException();
        }

        public string GetDataTypeName(int i)
        {
            throw new NotSupportedException();
        }

        public Type GetFieldType(int i)
        {
            throw new NotSupportedException();
        }

        public object GetValue(int i)
        {
            return current[i];
        }

        public int GetValues(object[] values)
        {
            throw new NotSupportedException();
        }

        public int GetOrdinal(string name)
        {
            throw new NotSupportedException();
        }

        public bool GetBoolean(int i)
        {
            throw new NotSupportedException();
        }

        public byte GetByte(int i)
        {
            throw new NotSupportedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        public char GetChar(int i)
        {
            throw new NotSupportedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotSupportedException();
        }

        public short GetInt16(int i)
        {
            throw new NotSupportedException();
        }

        public int GetInt32(int i)
        {
            throw new NotSupportedException();
        }

        public long GetInt64(int i)
        {
            throw new NotSupportedException();
        }

        public float GetFloat(int i)
        {
            throw new NotSupportedException();
        }

        public double GetDouble(int i)
        {
            throw new NotSupportedException();
        }

        public string GetString(int i)
        {
            throw new NotSupportedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotSupportedException();
        }

        public DateTime GetDateTime(int i)
        {
            throw new NotSupportedException();
        }

        public IDataReader GetData(int i)
        {
            throw new NotSupportedException();
        }

        public bool IsDBNull(int i)
        {
            throw new NotSupportedException();
        }

        public int FieldCount
        {
            get { return fieldCount; }
        }

        object IDataRecord.this[int i]
        {
            get { throw new NotSupportedException(); }
        }

        object IDataRecord.this[string name]
        {
            get { throw new NotSupportedException(); }
        }

        public void Close()
        {
            throw new NotSupportedException();
        }

        public DataTable GetSchemaTable()
        {
            throw new NotSupportedException();
        }

        public bool NextResult()
        {
            throw new NotSupportedException();
        }

        public bool Read()
        {
            currentRowIndex++;
            if (currentRowIndex == rowCount)
                return false;
            current = data[currentRowIndex];
            return true;
        }

        public int Depth
        {
            get { throw new NotSupportedException(); }
        }

        public bool IsClosed
        {
            get { throw new NotSupportedException(); }
        }

        public int RecordsAffected
        {
            get { throw new NotSupportedException(); }
        }
    }
}