using System;
using System.Linq;
using System.Linq.Expressions;
using Simple1C.Impl.Helpers;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Interface
{
    public static class Store1CHelpers
    {
        public static T Single<T>(this IDataContext dataContextFactory,
            params Expression<Func<T, bool>>[] filters)
            where T : Abstract1CEntity
        {
            var result = filters
                .Aggregate(dataContextFactory.Select<T>(), (q, f) => q.Where(f))
                .Take(2)
                .ToArray();
            if (result.Length == 0)
            {
                const string messageFormat = "can't find entity [{0}] by condition [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    typeof (T).FormatName(), filters.Select(Evaluator.PartialEval)
                        .JoinStrings(" && ")));
            }
            if (result.Length > 1)
            {
                const string messageFormat = "found more than one instance of entity [{0}] by condition [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    typeof (T).FormatName(), filters.Select(Evaluator.PartialEval)
                        .JoinStrings(" && ")));
            }
            return result[0];
        }

        public static T SingleOrDefault<T>(this IDataContext dataContextFactory,
            params Expression<Func<T, bool>>[] filters)
            where T : Abstract1CEntity
        {
            var result = filters
                .Aggregate(dataContextFactory.Select<T>(), (q, f) => q.Where(f))
                .Take(2)
                .ToArray();
            if (result.Length == 0)
                return null;
            if (result.Length > 1)
            {
                const string messageFormat = "found more than one instance of entity [{0}] by condition [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    typeof (T).FormatName(), filters.Select(Evaluator.PartialEval)
                        .JoinStrings(" && ")));
            }
            return result[0];
        }
    }
}