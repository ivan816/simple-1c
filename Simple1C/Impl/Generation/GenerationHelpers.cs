using System;
using System.Linq;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Generation
{
    internal static class GenerationHelpers
    {
        public static string IncrementIndent(string s)
        {
            return s.Split(new[] {"\r\n"}, StringSplitOptions.None)
                .Select(x => new string(' ', 4) + x)
                .JoinStrings("\r\n");
        }
    }
}