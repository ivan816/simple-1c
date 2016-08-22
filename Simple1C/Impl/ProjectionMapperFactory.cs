using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Helpers.MemberAccessor;
using Simple1C.Impl.Queriables;

namespace Simple1C.Impl
{
    internal static class ProjectionMapperFactory
    {
        private static readonly ConcurrentDictionary<string, Func<Projection, ComObjectMapper, Func<object, object>>>
            mappers =
                new ConcurrentDictionary<string, Func<Projection, ComObjectMapper, Func<object, object>>>();

        public static Func<object, object> GetMapper(Projection projection, ComObjectMapper mapper)
        {
            var cacheKey = projection.GetCacheKey();
            Func<Projection, ComObjectMapper, Func<object, object>> result;
            if (!mappers.TryGetValue(cacheKey, out result))
            {
                var argumentsExtractor = CreateArgumentsExtractor(projection);
                var instanceFactory = CreateInstanceFactory(projection);
                mappers.TryAdd(cacheKey, result = delegate(Projection currentProjection, ComObjectMapper currentMapper)
                {
                    var arguments = argumentsExtractor(currentProjection, currentMapper);
                    return queryResultRow => instanceFactory(arguments(queryResultRow));
                });
            }
            return result(projection, mapper);
        }

        private static Func<Projection, ComObjectMapper, Func<object, object[]>> CreateArgumentsExtractor(
            Projection projection)
        {
            var argumentsCount = 0;
            var parameterizer = new ParameterizingExpressionVisitor();
            foreach (var property in projection.properties)
            {
                var expression = property.expression;
                if (!property.needLocalEval)
                    continue;
                if (property.items.Length > argumentsCount)
                    argumentsCount = property.items.Length;
                var xParameter = Expression.Parameter(typeof(object[]));
                var xBody = parameterizer.Parameterize(expression, xParameter);
                var xConvertBody = Expression.Convert(xBody, typeof(object));
                var xLambda = Expression.Lambda<Func<object[], object>>(xConvertBody, xParameter);
                property.compiledExpression = xLambda.Compile();
            }
            return delegate(Projection currentProjection, ComObjectMapper currentMapper)
            {
                var fieldValues = new object[currentProjection.fields.Length];
                var propertyValues = new object[currentProjection.properties.Length];
                var propArguments = argumentsCount > 0 ? new object[argumentsCount] : null;
                return delegate(object queryResultRow)
                {
                    for (var i = 0; i < fieldValues.Length; i++)
                    {
                        var field = currentProjection.fields[i];
                        var fieldValue = field.GetValue(queryResultRow);
                        fieldValues[i] = currentMapper.MapFrom1C(fieldValue, field.Type);
                    }
                    for (var i = 0; i < currentProjection.properties.Length; i++)
                    {
                        var property = currentProjection.properties[i];
                        if (propArguments != null && property.needLocalEval)
                        {
                            for (var j = 0; j < property.items.Length; j++)
                                propArguments[j] = property.items[j].GetValue(fieldValues);
                            propertyValues[i] = projection.properties[i].compiledExpression(propArguments);
                        }
                        else
                            propertyValues[i] = property.items[0].GetValue(fieldValues);
                    }
                    return propertyValues;
                };
            };
        }

        private static readonly object[] emptyObjectArray = new object[0];

        private static Func<object[], object> CreateInstanceFactory(Projection projection)
        {
            if (projection.ctor == null)
                return a => a[0];
            var compiledCtorDelegate = ReflectionHelpers.GetCompiledDelegate(projection.ctor);
            if (projection.initMembers == null)
                return arguments => compiledCtorDelegate(null, arguments);
            if (projection.ctor.GetParameters().Length != 0)
                throw new InvalidOperationException("assertion exception");
            var memberAccessors = new MemberAccessor<object>[projection.initMembers.Length];
            for (var i = 0; i < memberAccessors.Length; i++)
                memberAccessors[i] = MemberAccessor<object>.Get(projection.initMembers[i]);
            return delegate(object[] arguments)
            {
                var result = compiledCtorDelegate(null, emptyObjectArray);
                for (var i = 0; i < memberAccessors.Length; i++)
                    memberAccessors[i].Set(result, arguments[i]);
                return result;
            };
        }
    }
}