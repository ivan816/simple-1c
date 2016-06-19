namespace Simple1C.Tests.TestEntities
{
    public class NomenclatureItem
    {
        public decimal Count { get; set; }
        public decimal Price { get; set; }
        public NdsRate NdsRate { get; set; }
        public decimal NdsSum { get; set; }
        public decimal Sum { get; set; }
        public string Name { get; set; }
    }
}