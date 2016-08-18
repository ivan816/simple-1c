using System;
using System.Collections.Generic;
using System.Reflection;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl
{
    internal class TypeRegistry
    {
        private readonly Dictionary<string, Type> typeMapping = new Dictionary<string, Type>();

        public TypeRegistry(Assembly assembly)
        {
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

        public Type GetTypeOrNull(string configurationName)
        {
            return typeMapping.GetOrDefault(configurationName);
        }
    }
}