using System;
using System.Runtime.InteropServices;

namespace Simple1C.Impl.Com
{
    public class DispatchObject
    {
        private readonly object comObject;

        protected DispatchObject(object comObject)
        {
            this.comObject = comObject;
        }

        protected string GetString(string property)
        {
            var value = Get(property);
            return Convert.IsDBNull(value) ? null : Convert.ToString(value);
        }

        protected object Get(string property)
        {
            return ComHelpers.GetProperty(comObject, property);
        }

        protected void Set(string property, object value)
        {
            ComHelpers.SetProperty(comObject, property, value);
        }

        protected object Invoke(string name, params object[] args)
        {
            return ComHelpers.Invoke(comObject, name, args);
        }

        protected object ComObject()
        {
            return comObject;
        }

        protected void Dispose()
        {
            Marshal.FinalReleaseComObject(comObject);
        }
    }
}