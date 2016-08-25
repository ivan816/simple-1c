using System;
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

        public static PropertyMapping Parse(string s)
        {
            var columnDesc = s.Split(new[] {" "}, StringSplitOptions.None);
            if (columnDesc.Length != 2 && columnDesc.Length != 3)
                throw new InvalidOperationException(string.Format("can't parse line [{0}]", s));
            return new PropertyMapping(columnDesc[0],
                columnDesc[1],
                columnDesc.Length == 3 ? columnDesc[2] : null);
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
    }
}