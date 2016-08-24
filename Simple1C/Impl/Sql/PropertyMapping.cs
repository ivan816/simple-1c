namespace Simple1C.Impl.Sql
{
    public class PropertyMapping
    {
        public string PropertyName { get; set; }
        public string FieldName { get; set; }
        public string TypeName { get; set; }

        public string GetDbFieldRef(string alias)
        {
            return alias + "." + FieldName;
        }
    }
}