using System;
using LinqTo1C.Impl.Com;

namespace LinqTo1C.Impl
{
    public class ComBasedEntityController : EntityController
    {
        private readonly ComObjectMapper comObjectMapper;

        public ComBasedEntityController(object comObject, ComObjectMapper comObjectMapper)
        {
            this.comObjectMapper = comObjectMapper;
            ComObject = comObject;
        }

        public object ComObject { get; private set; }

        protected override object GetValue(string name, Type type)
        {
            var propertyValue = ComHelpers.GetProperty(ComObject, name);
            return comObjectMapper.MapFrom1C(propertyValue, type);
        }
    }
}