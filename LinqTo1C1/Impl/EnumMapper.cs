using System;
using System.Collections.Concurrent;
using System.Linq;
using LinqTo1C.Impl.Com;

namespace LinqTo1C.Impl
{
    public class EnumMapper
    {
        private readonly GlobalContext globalContext;

        private static readonly ConcurrentDictionary<Type, MapItem[]> mappings =
            new ConcurrentDictionary<Type, MapItem[]>();

        public EnumMapper(GlobalContext globalContext)
        {
            this.globalContext = globalContext;
        }

        public object MapFrom1C(Type enumType, object value1C)
        {
            var enumerations = globalContext.Enumerations();
            var enumeration = ComHelpers.GetProperty(enumerations, enumType.Name);
            var valueIndex = Convert.ToInt32(ComHelpers.Invoke(enumeration, "IndexOf", value1C));
            var result = mappings.GetOrAdd(enumType, GetMappings)
                .SingleOrDefault(x => x.index == valueIndex);
            if (result == null)
            {
                const string messageFormat = "can't map value [{0}] to enum [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    globalContext.String(value1C), enumType.Name));
            }
            return result.value;
        }

        public object MapTo1C(object value)
        {
            var enumerations = globalContext.Enumerations();
            var enumeration = ComHelpers.GetProperty(enumerations, value.GetType().Name);
            return ComHelpers.GetProperty(enumeration, value.ToString());
        }

        private MapItem[] GetMappings(Type enumType)
        {
            var enumerations = globalContext.Enumerations();
            var enumeration = ComHelpers.GetProperty(enumerations, enumType.Name);
            return Enum.GetValues(enumType)
                .Cast<object>()
                .Select(v => new MapItem
                {
                    value = v,
                    index =
                        Convert.ToInt32(ComHelpers.Invoke(enumeration, "IndexOf",
                            ComHelpers.GetProperty(enumeration, v.ToString())))
                })
                .ToArray();
        }

        private class MapItem
        {
            public object value;
            public int index;
        }
    }
}