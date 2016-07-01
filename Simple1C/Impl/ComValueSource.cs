using System;
using Simple1C.Impl.Com;

namespace Simple1C.Impl
{
    internal class ComValueSource : IValueSource
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
            var isUniqueIdentifier = name == "УникальныйИдентификатор" && type == typeof(Guid?);
            var propertyValue = isUniqueIdentifier
                ? ComHelpers.Invoke(comObject, name)
                : ComHelpers.GetProperty(comObject, name);
            result = comObjectMapper.MapFrom1C(propertyValue, type);
            return true;
        }
    }
}