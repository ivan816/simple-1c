using System.Collections.Generic;

namespace Simple1C.Impl.Generation.Rendering
{
    internal class EnumFileModel
    {
        public EnumFileModel()
        {
            Items = new List<EnumItemModel>();
        }

        public string Namespace { get; set; }
        public string Name { get; set; }
        public List<EnumItemModel> Items { get; private set; }
    }
}