using System.Collections.Generic;
using Simple1C.Impl.Com;

namespace Simple1C.Impl.Queries
{
    public class ValueTableColumnCollection : DispatchObject
    {
        public ValueTableColumnCollection(object comObject)
            : base(comObject)
        {
        }

        public int Count
        {
            get { return (int) Invoke("Count"); }
        }

        public ValueTableColumn Get(int i)
        {
            return new ValueTableColumn(Invoke("Get", i));
        }

        public Dictionary<string, int> GetMap()
        {
            var result = new Dictionary<string, int>();
            for (var i = 0; i < Count; i++)
                result[Get(i).Name] = i;
            return result;
        }
    }
}