namespace Simple1C.Impl.Generation.Rendering
{
    internal class PropertyModel
    {
        public string PropertyName { get; set; }
        public int? MaxLength { get; set; }

        public string FieldName
        {
            get { return char.ToLower(PropertyName[0]) + PropertyName.Substring(1); }
        }

        public string Type { get; set; }
    }
}