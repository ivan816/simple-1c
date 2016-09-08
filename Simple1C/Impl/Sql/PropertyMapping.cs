using System;
using System.Linq;
using System.Text;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Sql
{
    internal class PropertyMapping
    {
        public PropertyMapping(string propertyName, PropertyType type,
            SingleColumnBinding singleBinding, UnionReferencesBinding unionBinding)
        {
            PropertyName = propertyName;
            Type = type;
            SingleBinding = singleBinding;
            UnionBinding = unionBinding;
        }

        public string Serialize()
        {
            var b = new StringBuilder();
            b.Append(PropertyName);
            b.Append(" ");
            b.Append(Type);
            switch (Type)
            {
                case PropertyType.Single:
                    b.Append(" ");
                    b.Append(SingleBinding.ColumnName);
                    if (!string.IsNullOrEmpty(SingleBinding.NestedTableName))
                    {
                        b.Append(" ");
                        b.Append(SingleBinding.NestedTableName);
                    }
                    break;
                case PropertyType.Union:
                    b.Append(" ");
                    b.Append(UnionBinding.TypeColumnName);
                    b.Append(" ");
                    b.Append(UnionBinding.TableIndexColumnName);
                    b.Append(" ");
                    b.Append(UnionBinding.ReferenceColumnName);
                    foreach (var t in UnionBinding.NestedTables)
                    {
                        b.Append(" ");
                        b.Append(t);
                    }
                    break;
                default:
                    throw new InvalidOperationException(string.Format("type [{0}] is not supported", Type));
            }
            return b.ToString();
        }

        public static PropertyMapping Parse(string s)
        {
            var columnDesc = s.Split(new[] {" "}, StringSplitOptions.None);
            if (columnDesc.Length < 2)
                throw new InvalidOperationException(string.Format("can't parse line [{0}]", s));
            var queryName = columnDesc[0];
            PropertyType propertyType;
            if (!Enum.TryParse(columnDesc[1], out propertyType))
            {
                const string messageFormat = "can't parse [{0}] from [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    typeof(PropertyType).FormatName(), s));
            }
            switch (propertyType)
            {
                case PropertyType.Single:
                    var singleInfo = new SingleColumnBinding(columnDesc[2],
                        columnDesc.Length >= 4 ? columnDesc[3] : null);
                    return new PropertyMapping(queryName, propertyType, singleInfo, null);
                case PropertyType.UnionReferences:
                    var unionInfo = new UnionReferencesBinding(columnDesc[2],
                        columnDesc[3], columnDesc[4],
                        columnDesc.Skip(5).ToArray());
                    return new PropertyMapping(queryName, propertyType, null, unionInfo);
                default:
                    throw new InvalidOperationException(string.Format("type [{0}] is not supported", propertyType));
            }
        }

        public string PropertyName { get; private set; }
        public PropertyType Type { get; private set; }
        public SingleColumnBinding SingleBinding { get; private set; }
        public UnionReferencesBinding UnionBinding { get; private set; }
    }
}