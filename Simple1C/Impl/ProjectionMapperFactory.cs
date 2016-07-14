using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Helpers.MemberAccessor;
using Simple1C.Impl.Queriables;

namespace Simple1C.Impl
{
    internal class ProjectionMapperFactory
    {
        private readonly ComObjectMapper comObjectMapper;

        private readonly ConcurrentDictionary<string, Func<object, Projection, object>> mappers =
            new ConcurrentDictionary<string, Func<object, Projection, object>>();

        public ProjectionMapperFactory(ComObjectMapper comObjectMapper)
        {
            this.comObjectMapper = comObjectMapper;
        }

        private static readonly object[] emptyObjectArray = new object[0];

        public Func<object, Projection, object> GetMapper(Projection projection)
        {
            var cacheKey = projection.GetCacheKey();
            Func<object, Projection, object> result;
            if (!mappers.TryGetValue(cacheKey, out result))
            {
                var compiledCtorDelegate = ReflectionHelpers.GetCompiledDelegate(projection.ctor);
                if (projection.initMembers != null)
                {
                    if (projection.ctor.GetParameters().Length != 0)
                        throw new InvalidOperationException("assertion exception");
                    var valuesFactory = CreatePropertyValuesFactory(projection);
                    var memberAccessors = new MemberAccessor<object>[projection.initMembers.Length];
                    for (var i = 0; i < memberAccessors.Length; i++)
                        memberAccessors[i] = MemberAccessor<object>.Get(projection.initMembers[i]);
                    result = delegate(object queryResultItem, Projection currentProjection)
                    {
                        var instance = compiledCtorDelegate(null, emptyObjectArray);
                        var propertyValues = valuesFactory(queryResultItem, currentProjection);
                        for (var i = 0; i < memberAccessors.Length; i++)
                            memberAccessors[i].Set(instance, propertyValues[i]);
                        return instance;
                    };
                }
                else
                {
                    var valuesFactory = CreatePropertyValuesFactory(projection);
                    result = (queryResultItem, currentProjection) =>
                        compiledCtorDelegate(null, valuesFactory(queryResultItem, currentProjection));
                }
                mappers.TryAdd(cacheKey, result);
            }
            return result;
        }

        private Func<object, Projection, object[]> CreatePropertyValuesFactory(Projection projection)
        {
            var propertyValueGetters = new Func<object[], object>[projection.properties.Length];
            var argumentsCount = 0;
            var parameterizer = new ParameterizingExpressionVisitor();
            for (var i = 0; i < propertyValueGetters.Length; i++)
            {
                var property = projection.properties[i];
                var expression = property.expression;
                if (property.items.Length == 1)
                    continue;
                if (property.items.Length > argumentsCount)
                    argumentsCount = property.items.Length;
                var xParameter = Expression.Parameter(typeof(object[]));
                var xBody = parameterizer.Parameterize(expression, xParameter);
                var xConvertBody = Expression.Convert(xBody, typeof(object));
                var xLambda = Expression.Lambda<Func<object[], object>>(xConvertBody, xParameter);
                propertyValueGetters[i] = xLambda.Compile();
            }
            return delegate(object queryResultItem, Projection currentProjection)
            {
                var fieldValues = new object[projection.fields.Length];
                for (var i = 0; i < fieldValues.Length; i++)
                {
                    var field = projection.fields[i];
                    var fieldValue = field.GetValue(queryResultItem);
                    fieldValues[i] = comObjectMapper.MapFrom1C(fieldValue, field.Type);
                }
                var propertyValues = new object[projection.properties.Length];
                var propArguments = argumentsCount > 0 ? new object[argumentsCount] : null;
                for (var i = 0; i < projection.properties.Length; i++)
                {
                    var property = projection.properties[i];
                    if (propArguments == null || property.items.Length == 1)
                        propertyValues[i] = currentProjection.GetValue(fieldValues, i, 0);
                    else
                    {
                        for (var j = 0; j < property.items.Length; j++)
                            propArguments[j] = currentProjection.GetValue(fieldValues, i, j);
                        var getter = propertyValueGetters[i];
                        propertyValues[i] = getter(propArguments);
                    }
                }
                return propertyValues;
            };
        }
    }
}