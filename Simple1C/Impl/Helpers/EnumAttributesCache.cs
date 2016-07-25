using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Simple1C.Impl.Helpers
{
    internal class EnumAttributesCache<TAttribute>
        where TAttribute : Attribute
    {
        public static readonly EnumAttributesCache<TAttribute> instance = new EnumAttributesCache<TAttribute>();

        private readonly ConcurrentDictionary<Type, IDictionary<string, TAttribute>> enumToItems =
            new ConcurrentDictionary<Type, IDictionary<string, TAttribute>>();

        public TAttribute GetAttribute<TEnum>(TEnum enumItem)
            where TEnum : struct
        {
            return GetAttributeUnsafe(enumItem);
        }

        public TAttribute GetAttributeUnsafe(object enumItem)
        {
            TAttribute result;
            if (!GetAllAttributes(enumItem.GetType())
                .TryGetValue(enumItem.ToString(), out result))
                throw new ArgumentOutOfRangeException(
                    "enumItem", string.Format("enum [{0}] has no [{1}] for [{2}]",
                        enumItem.GetType().FullName, typeof(TAttribute).Name, enumItem));
            return result;
        }

        public IDictionary<string, TAttribute> GetAllAttributes(Type enumType)
        {
            return enumToItems.GetOrAdd(enumType, GetEnumItems);
        }

        private static IDictionary<string, TAttribute> GetEnumItems(Type enumType)
        {
            return enumType
                .GetFields()
                .Where(item => !item.IsSpecialName)
                .Select(item => new {item.Name, Attr = GetEnumItemAttribute(item)})
                .Where(x => x.Attr != null)
                .ToDictionary(x => x.Name, x => x.Attr);
        }

        private static TAttribute GetEnumItemAttribute(FieldInfo enumItem)
        {
            return (TAttribute) enumItem
                .GetCustomAttributes(typeof(TAttribute), false)
                .FirstOrDefault();
        }
    }
}