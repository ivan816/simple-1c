using System;
using System.Collections.Generic;

namespace Simple1C.Impl.Helpers
{
    internal static class StringHelpers
    {
        public static bool EqualsIgnoringCase(this string s1, string s2)
        {
            return string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ContainsIgnoringCase(this string s1, string s2)
        {
            return s1.IndexOf(s2, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string ExcludeSuffix(this string s1, string suffix)
        {
            return s1 != null && s1.EndsWith(suffix)
                ? s1.Substring(0, s1.Length - suffix.Length)
                : s1;
        }

        public static IEnumerable<T> ParseLinesWithTabs<T>(string source, Func<string, List<string>, T> func)
        {
            var lines = source.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            var items = new List<string>();
            string headerLine = null;
            foreach (var s in lines)
            {
                if (s[0] == '\t')
                    items.Add(s.Substring(1));
                else
                {
                    if (headerLine != null)
                    {
                        yield return func(headerLine, items);
                        items.Clear();
                    }
                    headerLine = s;
                }
            }
            if (headerLine != null)
                yield return func(headerLine, items);
        }
    }
}