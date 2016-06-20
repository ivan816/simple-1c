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

        protected override object GetValue(string name, Type type)
        {
            var propertyValue = ComHelpers.GetProperty(ComObject, name);
            return comObjectMapper.MapFrom1C(propertyValue, type);
        }
    }
}