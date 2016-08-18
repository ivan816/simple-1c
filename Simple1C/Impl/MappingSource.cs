using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Simple1C.Impl
{
    internal class MappingSource
    {
        private static readonly ConcurrentDictionary<string, MappingSource> mappingsCache =
            new ConcurrentDictionary<string, MappingSource>();

        public static MappingSource Get(GlobalContext globalContext, Assembly assembly)
        {
            MappingSource result;
            var connectionString = globalContext.GetConnectionString();
            if (!mappingsCache.TryGetValue(connectionString, out result))
                mappingsCache.TryAdd(connectionString, result = new MappingSource(assembly));
            return result;
        }

        public MappingSource(Assembly assembly)
        {
            MetadataCache = new ConcurrentDictionary<ConfigurationName, Metadata>();
            EnumMappingsCache = new ConcurrentDictionary<Type, EnumMapItem[]>();
            TypeRegistry = new TypeRegistry(assembly);
        }

        public ConcurrentDictionary<ConfigurationName, Metadata> MetadataCache { get; private set; }
        public ConcurrentDictionary<Type, EnumMapItem[]> EnumMappingsCache { get; private set; }
        public TypeRegistry TypeRegistry { get; private set; }
    }
}