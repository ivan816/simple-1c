using System;
using System.Collections.Generic;
using Simple1C.Impl.Com;
using Simple1C.Impl.Generation;
using Simple1C.Impl.Queries;

namespace Simple1C.Impl
{
    internal class GlobalContext : DispatchObject, IDisposable
    {
        private object metadata;

        internal GlobalContext(object comObject) : base(comObject)
        {
        }

        public void BeginTransaction()
        {
            Invoke("BeginTransaction");
        }

        public void RollbackTransaction()
        {
            Invoke("RollbackTransaction");
        }

        public object Metadata
        {
            get { return metadata ?? (metadata = Get("Метаданные")); }
        }

        public string GetConnectionString()
        {
            return Convert.ToString(Invoke("СтрокаСоединенияИнформационнойБазы"));
        }

        public ConfigurationItem FindByName(ConfigurationName fullname)
        {
            var itemMetadata = ComHelpers.Invoke(Metadata, "НайтиПоПолномуИмени", fullname.Fullname);
            return new ConfigurationItem(fullname, itemMetadata);
        }

        public T NewObject<T>(string typeName) where T : DispatchObject
        {
            return (T) NewObject(typeof(T), typeName);
        }

        public object NewObject(Type type, string typeName)
        {
            if (!typeof(DispatchObject).IsAssignableFrom(type))
                throw new Exception(string.Format("Type {0} must be inherited from DispatchObject", type));
            return Activator.CreateInstance(type, Invoke("NewObject", typeName));
        }

        public QueryResult Execute(string query, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            var queryObject = NewObject<Query>("Query");
            queryObject.Text = query;
            foreach (var pair in parameters)
                queryObject.SetParameter(pair.Key, pair.Value);
            return queryObject.Execute();
        }

        public string String(object value)
        {
            return (string) Invoke("String", value);
        }

        public object РежимЗаписиДокумента()
        {
            return Get("РежимЗаписиДокумента");
        }

        public object GetManager(ConfigurationName name)
        {
            var scopeManager = Get(name.Scope.ToString());
            return ComHelpers.GetProperty(scopeManager, name.Name);
        }

        public new object ComObject()
        {
            return base.ComObject();
        }

        public new void Dispose()
        {
            base.Dispose();
        }

        private class Query : DispatchObject
        {
            public Query(object comObject)
                : base(comObject)
            {
            }

            public string Text
            {
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
    }
}