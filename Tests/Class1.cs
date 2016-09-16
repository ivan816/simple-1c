using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Parsing;
using Simple1C.Tests.Helpers;

namespace Simple1C.Tests
{
    internal class Class1 : TestBase
    {
        private static readonly Dictionary<string, string> keywordsMap =
          new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"выбрать", "select"},
                {"представление", "presentation"},
                {"как", "as"},
                {"из", "from"},
                {"где", "where"},
                {"и", "and"},
                {"или", "or"},
                {"датавремя", "datetime"},
                {"год", "year"},
                {"квартал", "quarter"},
                {"значение", "value"},
                {"ложь", "false"},
                {"истина", "true"},
                {"упорядочить", "order"},
                {"сгруппировать", "group"},
                {"по", "by"}
            };

        private static readonly Regex keywordsRegex = new Regex(string.Format(@"\b({0})\b",
            keywordsMap.Keys.JoinStrings("|")),
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        [Test]
        public void Test1()
        {
            var inputs = Directory.GetFiles("C:\\Users\\mskr\\Desktop\\queries")
                .Select(File.ReadAllText)
                .ToArray();
            var queryParser = new QueryParser();
            bool failed = false;
            foreach (var input in inputs)
            {
                try
                {
                    queryParser.Parse(keywordsRegex.Replace(input, m => keywordsMap[m.Groups[1].Value]));
                    Console.WriteLine("ok");
                }
                catch (Exception e)
                {
                    failed = true;
                    Console.WriteLine("Failed:");
                    Console.WriteLine(e);
                }
            }
            Assert.False(failed);
        }
    }
}