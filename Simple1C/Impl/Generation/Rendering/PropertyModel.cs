namespace Simple1C.Impl.Generation.Rendering
{
    public class PropertyModel
    {
        public string PropertyName { get; set; }

        public string FieldName
        {
            get { return char.ToLower(PropertyName[0]) + PropertyName.Substring(1); }
        }

        public string Type { get; set; }
    }
}