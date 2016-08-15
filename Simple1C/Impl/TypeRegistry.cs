using System;
using System.Collections.Generic;
using System.Reflection;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl
{
    internal class TypeRegistry
    {
        private static readonly Dictionary<string, Type> typeMapping = new Dictionary<string, Type>();
        private static readonly object lockObject = new object();
        private static readonly HashSet<Assembly> processedAssemblies = new HashSet<Assembly>();

        public TypeRegistry(Assembly assembly)
        {
            lock (lockObject)
            {
                if (!processedAssemblies.Add(assembly))
                    return;
                foreach (var x in assembly.GetTypes())
                {
                    if (!x.IsClass && !x.IsEnum)
                        continue;
                    var name = ConfigurationName.GetOrNull(x);
                    if (!name.HasValue)
                        continue;
                    typeMapping.Add(name.Value.Fullname, x);
                }
            }
        }

        public Type GetTypeOrNull(string configurationName)
        {
            lock (lockObject)
                return typeMapping.GetOrDefault(configurationName);
        }
    }
}