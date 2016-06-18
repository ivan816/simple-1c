using System;
using System.Linq;
using LinqTo1C.Impl.Helpers;

namespace LinqTo1C.Impl.Generation
{
    public static class GenerationHelpers
    {
        public static string IncrementIndent(string s)
        {
            return s.Split(new[] {"\r\n"}, StringSplitOptions.None)
                .Select(x => new string(' ', 4) + x)
                .JoinStrings("\r\n");
        }
    }
}