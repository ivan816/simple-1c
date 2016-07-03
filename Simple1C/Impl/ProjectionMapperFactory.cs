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
                            var memberAccessor = memberAccessors[i];
                            var fieldValue = ReadFieldValue(o, projection.fields[i], i, memberAccessor.MemberType);
                            memberAccessor.Set(instance, fieldValue);
                        }
                        return instance;
                    };
                }
                else
                {
                    var parameters = projection.ctor.GetParameters();
                    result = delegate(object o)
                    {
                        var arguments = new object[projection.fields.Length];
                        for (var i = 0; i < projection.fields.Length; i++)
                            arguments[i] = ReadFieldValue(o, projection.fields[i], i, parameters[i].ParameterType);
                        return compiledCtorDelegate(null, arguments);
                    };
                }
                mappers.TryAdd(cacheKey, result);
            }
            return result;
        }

        private object ReadFieldValue(object comObject, QueryField queryField, int fieldIndex, Type fieldType)
        {
            var result = ComHelpers.GetProperty(comObject, queryField.Alias);
            var isUniqueIdentifier =
                queryField.UniqueIdentifierFieldIndexes != null &&
                Array.IndexOf(queryField.UniqueIdentifierFieldIndexes, fieldIndex) >= 0;
            if (isUniqueIdentifier)
                result = ComHelpers.Invoke(result, EntityHelpers.idPropertyName);
            return comObjectMapper.MapFrom1C(result, fieldType);
        }
    }
}