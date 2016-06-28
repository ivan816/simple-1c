using System;
using Simple1C.Impl.Com;

namespace Simple1C.Impl.Queries
{
    public class QueryResultSelection
    {
        private readonly object comObject;

        internal QueryResultSelection(object comObject)
        {
            this.comObject = comObject;
        }

        public object ComObject
        {
            get { return comObject; }
        }

        public string GetString(string name)
        {
            return Convert.ToString(ComHelpers.GetProperty(comObject, name));
        }

        public object this[string name]
        {
            get { return ComHelpers.GetProperty(comObject, name); }
        }

        public bool Next()
        {
            return Convert.ToBoolean(ComHelpers.Invoke(comObject, "Следующий"));
        }

        public void Reset()
        {
            ComHelpers.Invoke(comObject, "Сбросить");
        }
    }
}