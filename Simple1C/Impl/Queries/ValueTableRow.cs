using System.Collections.Generic;
using Simple1C.Impl.Com;

namespace Simple1C.Impl.Queries
{
    internal class ValueTableRow : DispatchObject
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

        public new string GetString(string property)
        {
            return base.GetString(property);
        }

        public object Get(int i)
        {
            return Invoke("Get", i);
        }
    }
}