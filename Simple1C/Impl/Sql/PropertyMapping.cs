using System.Text;

namespace Simple1C.Impl.Sql
{
    public class PropertyMapping
    {
        public PropertyMapping(string propertyName, string fieldName, string nestedTableName)
        {
            PropertyName = propertyName;
            FieldName = fieldName;
            NestedTableName = nestedTableName;
            PatchFieldName();
        }

        private void PatchFieldName()
        {
            if (FieldName == "ID")
            {
                FieldName = "_idrref";
                return;
            }
            var b = new StringBuilder(FieldName);
            b[0] = char.ToLower(b[0]);
            b.Insert(0, '_');
            if (!string.IsNullOrEmpty(NestedTableName))
                b.Append("rref");
            FieldName = b.ToString();
        }

        public string PropertyName { get; private set; }
        public string FieldName { get; private set; }
        public string NestedTableName { get; private set; }
        public TableMapping NestedTableMapping { get; set; }
    }
}