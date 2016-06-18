using System.Collections.Generic;

namespace LinqTo1C.Impl.Generation
{
    public class FormatParameters
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>();

        public FormatParameters With(string name, string value)
        {
            values.Add(name, value);
            return this;
        }

        public bool TryGet(string name, out string value)
        {
            return values.TryGetValue(name, out value);
        }
    }
}