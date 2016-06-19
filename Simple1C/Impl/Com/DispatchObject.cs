using System;

namespace Simple1C.Impl.Com
{
    internal class DispatchObject
    {
        internal object ComObject { get; private set; }

        protected internal DispatchObject(object comObject)
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