using System;
using System.Collections;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl
{
    internal class DictionaryBasedEntityController : EntityController
    {
        protected override object GetValue(string name, Type type)
        {
            var result = Changed == null ? type.GetDefaultValue() : Changed.GetOrDefault(name);
            if (result == null && typeof (IList).IsAssignableFrom(type))
            {
                var listItemType = type.GetGenericArguments()[0];
                result = ListFactory.Create(listItemType, null, 1);
                MarkAsChanged(name, result);
            }
            return result;
        }
    }
}