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
            Assembly = assembly;
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

        public Assembly Assembly { get; private set; }

        public Type GetTypeOrNull(string configurationName)
        {
            return typeMapping.GetOrDefault(configurationName);
        }

        public Type GetType(string typeName)
        {
            var type = GetTypeOrNull(typeName);
            if (type == null)
            {
                const string messageFormat = "can't find .NET type by 1c type [{0}], mappings assembly [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    typeName, Assembly.GetName().Name));
            }
            return type;
        }
    }
}