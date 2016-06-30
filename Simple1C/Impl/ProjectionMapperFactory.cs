using System;
using System.Collections.Concurrent;
using Simple1C.Impl.Com;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Helpers.MemberAccessor;
using Simple1C.Impl.Queriables;

namespace Simple1C.Impl
{
    internal class ProjectionMapperFactory
    {
        private readonly ComObjectMapper comObjectMapper;

        private readonly ConcurrentDictionary<string, Func<object, object>> mappers =
            new ConcurrentDictionary<string, Func<object, object>>();

        public ProjectionMapperFactory(ComObjectMapper comObjectMapper)
        {
            this.comObjectMapper = comObjectMapper;
        }

        private static readonly object[] emptyObjectArray = new object[0];

        public Func<object, object> GetMapper(Projection projection)
        {
            var cacheKey = projection.GetCacheKey();
            Func<object, object> result;
            if (!mappers.TryGetValue(cacheKey, out result))
            {
                var compiledCtorDelegate = ReflectionHelpers.GetCompiledDelegate(projection.ctor);
                if (projection.initMembers != null)
                {
                    if (projection.ctor.GetParameters().Length != 0)
                        throw new InvalidOperationException("assertion exception");
                    var memberAccessors = new MemberAccessor<object>[projection.initMembers.Length];
                    for (var i = 0; i < memberAccessors.Length; i++)
                        memberAccessors[i] = MemberAccessor<object>.Get(projection.initMembers[i]);
                    result = delegate(object o)
                    {
                        var instance = compiledCtorDelegate(null, emptyObjectArray);
                        for (var i = 0; i < memberAccessors.Length; i++)
                        {
                            var value = ComHelpers.GetProperty(o, projection.aliasFieldNames[i]);
                            var memberAccessor = memberAccessors[i];
                            memberAccessor.Set(instance, comObjectMapper.MapFrom1C(value, memberAccessor.MemberType));
                        }
                        return instance;
                    };
                }
                else
                {
                    var parameters = projection.ctor.GetParameters();
                    result = delegate(object o)
                    {
                        var arguments = new object[projection.aliasFieldNames.Length];
                        for (var i = 0; i < projection.aliasFieldNames.Length; i++)
                        {
                            var value = ComHelpers.GetProperty(o, projection.aliasFieldNames[i]);
                            arguments[i] = comObjectMapper.MapFrom1C(value, parameters[i].ParameterType);
                        }
                        return compiledCtorDelegate(null, arguments);
                    };    
                }
                mappers.TryAdd(cacheKey, result);
            }
            return result;
        }
    }
}