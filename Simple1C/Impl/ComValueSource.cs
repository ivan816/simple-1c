using System;
using Simple1C.Impl.Com;

namespace Simple1C.Impl
{
    public class ComValueSource : IValueSource
    {
        private readonly ComObjectMapper comObjectMapper;
        private readonly object comObject;

        internal ComValueSource(object comObject, ComObjectMapper comObjectMapper, bool writable)
        {
            this.comObjectMapper = comObjectMapper;
            this.comObject = comObject;
            Writable = writable;
        }

        public object GetBackingStorage()
        {
            return comObject;
        }

        public bool Writable { get; private set; }

        bool IValueSource.TryLoadValue(string name, Type type, out object result)
        {
            var propertyValue = ComHelpers.GetProperty(comObject, name);
            result = comObjectMapper.MapFrom1C(propertyValue, type);
            return true;
        }
    }
}