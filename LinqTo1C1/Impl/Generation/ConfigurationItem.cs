namespace LinqTo1C.Impl.Generation
{
    public class ConfigurationItem
    {
        public ConfigurationItem(string fullname, object comObject)
        {
            Name = ConfigurationName.Parse(fullname);
            ComObject = comObject;
        }

        public ConfigurationName Name { get; private set; }
        public object ComObject { get; private set; }
    }
}