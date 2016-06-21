using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl
{
    internal class TypeMapper
    {
        private readonly Dictionary<string, Type> typeMapping;

        public TypeMapper(Assembly assembly)
        {
            typeMapping = assembly.GetTypes()
                .Where(x => x.IsClass || x.IsEnum)
                .Select(x => new
                {
                    typeName1C = ConfigurationName.GetOrNull(x),
                    type = x
                })
                .Where(x => x.typeName1C.HasValue)
                .ToDictionary(x => x.typeName1C.Value.Fullname, x => x.type);
        }

        public Type GetTypeOrNull(string configurationName)
        {
            return typeMapping.GetOrDefault(configurationName);
        }
    }
}