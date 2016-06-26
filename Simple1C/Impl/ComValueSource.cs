using System;
using Simple1C.Impl.Com;

namespace Simple1C.Impl
{
    public class ComValueSource : IValueSource
    {
        private readonly ComObjectMapper comObjectMapper;
        private readonly object comObject;

        internal ComValueSource(object comObject, ComObjectMapper comObjectMapper)
        {
            this.comObjectMapper = comObjectMapper;
            this.comObject = comObject;
        }

        public object GetBackingStorage()
        {
            return comObject;
        }

        bool IValueSource.TryLoadValue(string name, Type type, out object result)
        {
            var propertyValue = ComHelpers.GetProperty(comObject, name);
            result = comObjectMapper.MapFrom1C(propertyValue, type);
            return true;
        }
    }
}