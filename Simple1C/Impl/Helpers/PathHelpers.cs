using System;
using System.IO;

namespace Simple1C.Impl.Helpers
{
    internal static class PathHelpers
    {
        public static string AppendBasePath(string path)
        {
            return Path.Combine(BasePath(), path);
        }

        public static string BasePath()
        {
            var domain = AppDomain.CurrentDomain;
            var useRelativeSearchPath = !string.IsNullOrEmpty(domain.RelativeSearchPath) &&
                                        domain.RelativeSearchPath.StartsWith(
                                            domain.BaseDirectory.ExcludeTrailingSlash(),
                                            StringComparison.OrdinalIgnoreCase);
            return useRelativeSearchPath ? domain.RelativeSearchPath : domain.BaseDirectory;
        }

        public static string ExcludeTrailingSlash(this string s)
        {
            return string.IsNullOrEmpty(s) || s[s.Length - 1] != '\\' ? s : s.Substring(0, s.Length - 1);
        }

        public static string GetFileName(string p)
        {
            var result = Path.GetFileName(p);
            if (result == null)
                throw new InvalidOperationException("assertion failiure");
            return result;
        }

        public static string GetDirectoryName(string p)
        {
            var resul = Path.GetDirectoryName(p);
            if (resul == null)
                throw new InvalidOperationException("assertion failure");
            return resul;
        }
    }
}