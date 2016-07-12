using System.Collections.Generic;

namespace Simple1C.Impl.Generation.Rendering
{
    public class EnumFileModel
    {
        public EnumFileModel()
        {
            Items = new List<string>();
        }

        public string Namespace { get; set; }
        public string Name { get; set; }
        public List<string> Items { get; private set; }
    }
}