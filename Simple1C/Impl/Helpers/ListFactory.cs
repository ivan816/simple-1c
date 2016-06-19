using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Simple1C.Impl.Helpers
{
    internal static class ListFactory
    {
        private static readonly ConcurrentDictionary<Type, IFactory> factories =
            new ConcurrentDictionary<Type, IFactory>();

        private static readonly Func<Type, IFactory> createFactory = CreateFactory;

        public static IList Create(Type itemType, IList source, int capacity)
        {
            return factories.GetOrAdd(itemType, createFactory).Create(source, capacity);
        }

        private static IFactory CreateFactory(Type itemType)
        {
            return (IFactory) Activator.CreateInstance(typeof (Factory<>).MakeGenericType(itemType));
        }

        private interface IFactory
        {
            IList Create(IList source, int capacity);
        }

        private class Factory<T> : IFactory
        {
            public IList Create(IList source, int capacity)
            {
                return source == null
                    ? new List<T>(capacity)
                    : new List<T>((IEnumerable<T>) source);
            }
        }
    }
}