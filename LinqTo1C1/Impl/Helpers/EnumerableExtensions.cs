using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace LinqTo1C.Impl.Helpers
{
    public static class NameValueCollectionHelpers
    {
        public static NameValueCollection ParseCommandLine(IEnumerable<string> args)
        {
            var result = new NameValueCollection();
            var state = CommandLineParsingState.WaitingTerm;
            string key = null;
            foreach (var argument in args)
                switch (state)
                {
                    case CommandLineParsingState.WaitingTerm:
                        if (IsKey(argument))
                        {
                            key = GetKey(argument);
                            state = CommandLineParsingState.WaitingValue;
                        }
                        else
                            throw new InvalidOperationException(
                                string.Format("Expected term (must start from '-' or '/') but actual was '{0}'",
                                    argument));
                        break;
                    case CommandLineParsingState.WaitingValue:
                        if (IsKey(argument))
                        {
                            result.Add(key, "true");
                            key = GetKey(argument);
                            state = CommandLineParsingState.WaitingValue;
                        }
                        else
                        {
                            result.Add(key, argument);
                            key = null;
                            state = CommandLineParsingState.WaitingTerm;
                        }
                        break;
                }
            if (state == CommandLineParsingState.WaitingValue)
                result.Add(key, "true");
            return result;
        }

        private static bool IsKey(string argument)
        {
            return argument.StartsWith("-") || argument.StartsWith("/");
        }

        private static string GetKey(string argument)
        {
            return argument.Substring(1);
        }

        private enum CommandLineParsingState
        {
            WaitingTerm,
            WaitingValue
        }
    }
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