using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Simple1C.Impl.Helpers
{
    internal static class ExceptionHelpers
    {
        public static bool IsCancellation(this Exception exception)
        {
            return exception.Unwrap().All(x => Cause(x) is OperationCanceledException);
        }

        public static IEnumerable<Exception> WithNested(this Exception exception)
        {
            var e = exception;
            while (e != null)
            {
                yield return e;
                e = e.InnerException;
            }
        }

        public static Exception Cause(this Exception exception)
        {
            return exception.WithNested().Last();
        }

        public static IEnumerable<Exception> Unwrap(this Exception exception)
        {
            var aggregateException = exception as AggregateException;
            if (aggregateException != null)
                return aggregateException.InnerExceptions.SelectMany(Unwrap);
            var targetInvokationException = exception as TargetInvocationException;
            return targetInvokationException != null
                ? Unwrap(targetInvokationException.InnerException)
                : Enumerable.Repeat(exception, 1);
        }
    }
}