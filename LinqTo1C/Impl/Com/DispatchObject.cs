using System;

namespace LinqTo1C.Impl.Com
{
    public class DispatchObject
    {
        public object ComObject { get; private set; }

        public DispatchObject(object comObject)
        {
            ComObject = comObject;
        }

        protected string GetString(string property)
        {
            var value = Get(property);
            return Convert.IsDBNull(value) ? null : Convert.ToString(value);
        }

        protected object Get(string property)
        {
            return ComHelpers.GetProperty(ComObject, property);
        }

        protected void Set(string property, object value)
        {
            ComHelpers.SetProperty(ComObject, property, value);
        }

        protected object Invoke(string name, params object[] args)
        {
            return ComHelpers.Invoke(ComObject, name, args);
        }
    }
}