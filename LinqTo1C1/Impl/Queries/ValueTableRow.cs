using System.Collections.Generic;
using LinqTo1C.Impl.Com;

namespace LinqTo1C.Impl.Queries
{
    public class ValueTableRow : DispatchObject
    {
        private readonly Dictionary<string, int> columnsMap;

        public ValueTableRow(object comObject, Dictionary<string, int> columnsMap)
            : base(comObject)
        {
            this.columnsMap = columnsMap;
        }

        public object this[string name]
        {
            get { return Get(columnsMap[name]); }
        }

        public object Get(int i)
        {
            return Invoke("Get", i);
        }
    }
}