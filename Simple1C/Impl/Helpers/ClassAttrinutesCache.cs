using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Simple1C.Impl.Helpers
{
    internal class ClassAttrinutesCache<TAttribute> where TAttribute : Attribute
    {
        public static readonly ClassAttrinutesCache<TAttribute> instance = new ClassAttrinutesCache<TAttribute>();

        private readonly ConcurrentDictionary<Type, TAttribute> classAttributes =
            new ConcurrentDictionary<Type, TAttribute>();

        public TAttribute GetAttribute(Type type)
        {
            TAttribute attribute;
            if (!classAttributes.TryGetValue(type, out attribute))
            {
                var attributes = type.GetCustomAttributes(typeof(TAttribute), true)
                    .Cast<TAttribute>()
                    .ToList();
                if (attributes.Count == 0)
                {
                    const string messageFormat = "class [{0}] has no attribute [{1}]";
                    throw new ArgumentOutOfRangeException(string.Format(messageFormat, type.Name,
                        typeof(TAttribute).Name));
                }
                if (attributes.Count > 1)
                {
                    const string messageFormat = "class [{0}] has more then one attribute [{1}]";
                    throw new ArgumentOutOfRangeException(string.Format(messageFormat, type.Name,
                        typeof(TAttribute).Name));
                }
                attribute = attributes[0];
                classAttributes.TryAdd(type, attribute);
            }
            return attribute;
        }
    }
}