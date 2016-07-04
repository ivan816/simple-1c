namespace Simple1C.Impl.Generation.Rendering
{
    public class ClassFileModel
    {
        public string Namespace { get; set; }
        public string ConfigurationScope { get; set; }
        public ClassModel MainClass { get; set; }
        public ClassModel NestedClasses { get; set; }
    }
}