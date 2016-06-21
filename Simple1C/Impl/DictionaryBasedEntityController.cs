using System;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl
{
    internal class DictionaryBasedEntityController : EntityController
    {
        protected override bool TryGetValue(string name, Type type, out object result)
        {
            if (Changed == null)
            {
                result = null;
                return false;
            }
            return Changed.TryGetValue(name, out result);
        }
    }
}