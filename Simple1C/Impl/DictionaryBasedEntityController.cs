using System;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl
{
    internal class DictionaryBasedEntityController : EntityController
    {
        protected override object GetValue(string name, Type type)
        {
            return Changed == null ? type.GetDefaultValue() : Changed.GetOrDefault(name);
        }
    }
}