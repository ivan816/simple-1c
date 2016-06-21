using System;
using Simple1C.Impl.Com;

namespace Simple1C.Impl
{
    internal class ComBasedEntityController : EntityController
    {
        private readonly ComObjectMapper comObjectMapper;

        public ComBasedEntityController(object comObject, ComObjectMapper comObjectMapper)
        {
            this.comObjectMapper = comObjectMapper;
            ComObject = comObject;
        }

        protected override bool TryGetValue(string name, Type type, out object result)
        {
            var propertyValue = ComHelpers.GetProperty(ComObject, name);
            result = comObjectMapper.MapFrom1C(propertyValue, type);
            return true;
        }
    }
}