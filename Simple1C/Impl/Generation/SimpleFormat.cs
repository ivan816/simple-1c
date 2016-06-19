using System;
using System.Collections.Generic;
using System.Text;

namespace Simple1C.Impl.Generation
{
    internal class SimpleFormat
    {
        private readonly List<Part> parts;

        private SimpleFormat(List<Part> parts)
        {
            this.parts = parts;
        }

        public string Apply(FormatParameters p)
        {
            var builder = new StringBuilder();
            foreach (var part in parts)
            {
                var s = part.name;
                if (part.isParameter && !p.TryGet(s, out s))
                    throw new InvalidOperationException(string.Format("parameter [{0}] required", part.name));
                builder.Append(s);
            }
            return builder.ToString();
        }

        public static SimpleFormat Parse(string source)
        {
            if (source.StartsWith("\r\n"))
                source = source.Substring(2);
            var parts = new List<Part>();
            var position = 0;
            int openIndex;
            while (position < source.Length && (openIndex = SearchDelimiter(source, position)) >= 0)
            {
                var closeIndex = SearchDelimiter(source, openIndex + 1);
                if (closeIndex < 0 || closeIndex <= openIndex + 1)
                    throw new InvalidOperationException(string.Format("invalid format string [{0}]", source));
                if (openIndex > position)
                    parts.Add(new Part
                    {
                        name = source.Substring(position, openIndex - position),
                        isParameter = false
                    });
                parts.Add(new Part
                {
                    name = source.Substring(openIndex + 1, closeIndex - openIndex - 1),
                    isParameter = true
                });
                position = closeIndex + 1;
            }
            if (position < source.Length - 1)
                parts.Add(new Part
                {
                    name = source.Substring(position),
                    isParameter = false
                });
            return new SimpleFormat(parts);
        }

        private static int SearchDelimiter(string s, int startFrom)
        {
            return s.IndexOf("%", startFrom, StringComparison.OrdinalIgnoreCase);
        }

        private class Part
        {
            public string name;
            public bool isParameter;
        }
    }
}