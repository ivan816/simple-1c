using System.Collections.Generic;
using System.Linq;

namespace LinqTo1C.Impl.Helpers
{
    public static class EnumerableExtensions
    {
        public static string JoinStrings<T>(this IEnumerable<T> source, string separator)
        {
            return string.Join(separator, source);
        }

        public static IEnumerable<T> NotNull<T>(this IEnumerable<T> source) where T : class
        {
            return source.Where(x => x != null);
        }

        public static HashSet<T> ToSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
        {
            return new HashSet<T>(source, comparer);
        }
    }
}