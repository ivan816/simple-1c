using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Com;

namespace Simple1C.Impl.Queries
{
    internal class ValueTable : DispatchObject, IEnumerable<ValueTableRow>
    {
        private readonly Dictionary<string, int> columnsMap;

        public ValueTable(object comObject) : base(comObject)
        {
            columnsMap = GetColumns().GetMap();
        }

        public ValueTableColumnCollection GetColumns()
        {
            return new ValueTableColumnCollection(Get("Columns"));
        }

        public int Count
        {
            get { return (int) Invoke("Count"); }
        }

        public ValueTableRow this[int index]
        {
            get { return Get(index); }
        }

        public ValueTableRow Get(int i)
        {
            return new ValueTableRow(Invoke("Get", i), columnsMap);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<ValueTableRow> GetEnumerator()
        {
            return Enumerable.Range(0, Count).Select(x => this[x]).GetEnumerator();
        }
    }
}