namespace Simple1C.Impl.Generation
{
    internal class ConfigurationItem
    {
        public ConfigurationItem(ConfigurationName name, object comObject)
        {
            Name = name;
            ComObject = comObject;
        }

        public ConfigurationName Name { get; private set; }
        public object ComObject { get; private set; }
    }
}