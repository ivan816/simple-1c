using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Simple1C.Impl.Helpers
{
    public class AttributesCache
    {
        private static readonly ConcurrentDictionary<Key, object> cache = new ConcurrentDictionary<Key, object>();

        private static readonly Func<Key, object> createDelegate =
            key => key.attributeProvider.GetCustomAttributes(key.attributeType, key.inherit);

        public static object GetCustomAttributes(ICustomAttributeProvider attributeProvider, Type attributeType,
            bool inherit)
        {
            return cache.GetOrAdd(new Key(attributeProvider, attributeType, inherit), createDelegate);
        }

        public static T GetCustomAttribute<T>(ICustomAttributeProvider attributeProvider, bool inherit)
        {
            var attributes = (T[]) GetCustomAttributes(attributeProvider, typeof(T), inherit);
            if (attributes.Length == 0)
            {
                const string messageFormat = "[{0}] has no attribute [{1}]";
                throw new ArgumentOutOfRangeException(string.Format(messageFormat,
                    FormatName(attributeProvider), typeof(T).Name));
            }
            if (attributes.Length > 1)
            {
                const string messageFormat = "[{0}] has more then one attribute [{1}]";
                throw new ArgumentOutOfRangeException(string.Format(messageFormat,
                    FormatName(attributeProvider), typeof(T).Name));
            }
            return attributes[0];
        }

        private static string FormatName(ICustomAttributeProvider attributeProvider)
        {
            var t = (Type) attributeProvider;
            if (t != null)
                return t.FormatName();
            var m = (MemberInfo) attributeProvider;
            if (m != null)
                return m.DeclaringType.FormatName() + "." + m.Name;
            return attributeProvider.ToString();
        }

        private struct Key : IEquatable<Key>
        {
            public readonly ICustomAttributeProvider attributeProvider;
            public readonly Type attributeType;
            public readonly bool inherit;

            public Key(ICustomAttributeProvider attributeProvider, Type attributeType, bool inherit)
            {
                this.attributeProvider = attributeProvider;
                this.attributeType = attributeType;
                this.inherit = inherit;
            }

            public bool Equals(Key other)
            {
                var localInherit = inherit;
                return attributeProvider.Equals(other.attributeProvider) &&
                       attributeType == other.attributeType &&
                       localInherit.Equals(other.inherit);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is Key && Equals((Key) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (attributeProvider != null ? attributeProvider.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (attributeType != null ? attributeType.GetHashCode() : 0);
                    var localInherit = inherit;
                    hashCode = (hashCode*397) ^ localInherit.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}