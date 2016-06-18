using System;
using System.Collections.Generic;
using LinqTo1C.Impl.Com;
using LinqTo1C.Impl.Queries;

namespace LinqTo1C.Impl
{
    public class GlobalContext : DispatchObject
    {
        public GlobalContext(object comObject) : base(comObject)
        {
        }

        public T NewObject<T>(string typeName) where T : DispatchObject
        {
            return (T) NewObject(typeof (T), typeName);
        }

        public object NewObject(Type type, string typeName)
        {
            if (!typeof (DispatchObject).IsAssignableFrom(type))
                throw new Exception(string.Format("Type {0} must be inherited from DispatchObject", type));

            //todo reflection.emit?
            return Activator.CreateInstance(type, Invoke("NewObject", typeName));
        }

        public ValueTable Execute(string query, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            var queryObject = NewObject<Query>("Query");
            queryObject.Text = query;
            foreach (var pair in parameters)
                queryObject.SetParameter(pair.Key, pair.Value);
            return queryObject.Execute().Unload();
        }

        public string String(object value)
        {
            return (string) Invoke("String", value);
        }

        public object Enumerations()
        {
            return ComHelpers.GetProperty(ComObject, "Перечисления");
        }

        private class Query : DispatchObject
        {
            public Query(object comObject)
                : base(comObject)
            {
            }

            public string Text
            {
                get { return GetString("Text"); }
                set { Set("Text", value); }
            }

            public void SetParameter(string key, object value)
            {
                Invoke("SetParameter", key, value);
            }

            public QueryResult Execute()
            {
                return new QueryResult(Invoke("Execute"));
            }
        }

        private class QueryResult : DispatchObject
        {
            public QueryResult(object comObject)
                : base(comObject)
            {
            }

            public ValueTable Unload()
            {
                return new ValueTable(Invoke("Unload"));
            }
        }
    }
}