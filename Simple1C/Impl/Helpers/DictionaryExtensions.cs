using System;
using System.Collections.Generic;

namespace Simple1C.Impl.Helpers
{
    internal static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
            Func<TKey, TValue> valueFactory)
        {
            TValue result;
            if (!dictionary.TryGetValue(key, out result))
                dictionary.Add(key, result = valueFactory(key));
            return result;
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue result;
            return dictionary.TryGetValue(key, out result) ? result : default(TValue);
        }
    }
}