using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Simple1C.Impl.Helpers
{
    internal static class PathHelpers
    {
        public static string GetRelativePath(string fromPath, string toPath)
        {
            if (!Path.IsPathRooted(toPath))
                return toPath;
            var fromPathArray = GetPathArray(Path.GetFullPath(fromPath));
            var toPathArray = GetPathArray(Path.GetFullPath(toPath));
            var resultBuilder = new StringBuilder();

            var firstDifferentIndex = GetFirstDifferentIndex(fromPathArray, toPathArray);
            MoveFromPathUp(fromPathArray, firstDifferentIndex, resultBuilder);
            MoveToPathDown(toPathArray, firstDifferentIndex, resultBuilder);
            return ExcludeTrailingSlash(resultBuilder.ToString());
        }

        private static string[] GetPathArray(string path)
        {
            return path.Split(Path.DirectorySeparatorChar);
        }

        public static string IncludeTrailingDirectorySlash(string source)
        {
            if (source.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return source;
            if (source.EndsWith("/"))
                return source.Substring(0, source.Length - 1) + Path.DirectorySeparatorChar;
            return source + Path.DirectorySeparatorChar;
        }

        private static void MoveToPathDown(string[] toPathArray, int startIndex, StringBuilder resultBuilder)
        {
            for (var i = startIndex; i < toPathArray.Length; i++)
                resultBuilder.AppendFormat("{0}{1}", toPathArray[i], Path.DirectorySeparatorChar);
        }

        private static void MoveFromPathUp(ICollection<string> fromPathArray, int startIndex,
                                           StringBuilder resultBuilder)
        {
            for (var i = startIndex; i < fromPathArray.Count - 1; i++)
                resultBuilder.Append(string.Format("..{0}", Path.DirectorySeparatorChar));
        }

        private static int GetFirstDifferentIndex(string[] fromPathArray, string[] toPathArray)
        {
            var result = 0;
            while (result < fromPathArray.Length && result < toPathArray.Length &&
                   StringComparer.OrdinalIgnoreCase.Compare(fromPathArray[result], toPathArray[result]) == 0)
                result++;
            return result;
        }

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
            return string.IsNullOrEmpty(s) || s[s.Length - 1] != '\\' 
                ? s 
                : s.Substring(0, s.Length - 1);
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