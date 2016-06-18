using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LinqTo1C.Impl.Com;

namespace LinqTo1C.Impl.Queries
{
    public class ValueTable : DispatchObject, IEnumerable<ValueTableRow>
    {
        private readonly Dictionary<string, int> columnsMap;

        public ValueTable(object comObject) : base(comObject)
        {
            columnsMap = Columns.GetMap();
        }

        public ValueTableColumnCollection Columns
        {
            get { return new ValueTableColumnCollection(Get("Columns")); }
        }

        public int Count
        {
            get { return (int) Get("Count"); }
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