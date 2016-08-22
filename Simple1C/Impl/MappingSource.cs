using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Simple1C.Impl
{
    internal class MappingSource
    {
        private static readonly ConcurrentDictionary<string, MappingSource> mappingsCache =
            new ConcurrentDictionary<string, MappingSource>();

        public static MappingSource Map(GlobalContext globalContext, Assembly assembly)
        {
            MappingSource result;
            var connectionString = globalContext.GetConnectionString();
            if (!mappingsCache.TryGetValue(connectionString, out result))
                mappingsCache.TryAdd(connectionString, result = new MappingSource(assembly));
            else if (!ReferenceEquals(result.TypeRegistry.Assembly, assembly))
            {
                const string messageFormat = "can't map [{0}] to [{1}] because it's already mapped to [{2}]";
                throw new InvalidOperationException(string.Format(messageFormat, connectionString,
                    assembly.GetName().Name, result.TypeRegistry.Assembly.GetName().Name));
            }
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