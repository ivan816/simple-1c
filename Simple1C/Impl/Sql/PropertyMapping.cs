using System;
using System.Linq;
using System.Text;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Sql
{
    internal class PropertyMapping
    {
        public PropertyMapping(string propertyName, PropertyKind kind,
            SingleColumnBinding singleBinding, UnionReferencesBinding unionBinding)
        {
            PropertyName = propertyName;
            Kind = kind;
            SingleBinding = singleBinding;
            UnionBinding = unionBinding;
        }

        public string Serialize()
        {
            var b = new StringBuilder();
            b.Append(PropertyName);
            b.Append(" ");
            b.Append(Kind);
            switch (Kind)
            {
                case PropertyKind.Single:
                    b.Append(" ");
                    b.Append(SingleBinding.ColumnName);
                    b.Append(" ");
                    b.Append(SingleBinding.NestedTableName);
                    break;
                case PropertyKind.UnionReferences:
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
                    throw new InvalidOperationException(string.Format("type [{0}] is not supported", Kind));
            }
            return b.ToString();
        }

        public static PropertyMapping Parse(string s)
        {
            var columnDesc = s.Split(new[] {" "}, StringSplitOptions.None);
            if (columnDesc.Length < 2)
                throw new InvalidOperationException(string.Format("can't parse line [{0}]", s));
            var queryName = columnDesc[0];
            PropertyKind propertyKind;
            if (!Enum.TryParse(columnDesc[1], out propertyKind))
            {
                const string messageFormat = "can't parse [{0}] from [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    typeof(PropertyKind).FormatName(), s));
            }
            switch (propertyKind)
            {
                case PropertyKind.Single:
                    var singleInfo = new SingleColumnBinding(columnDesc[2],
                        columnDesc.Length >= 4 ? columnDesc[3] : null);
                    return new PropertyMapping(queryName, propertyKind, singleInfo, null);
                case PropertyKind.UnionReferences:
                    var unionInfo = new UnionReferencesBinding(columnDesc[2],
                        columnDesc[3], columnDesc[4],
                        columnDesc.Skip(5).ToArray());
                    return new PropertyMapping(queryName, propertyKind, null, unionInfo);
                default:
                    throw new InvalidOperationException(string.Format("type [{0}] is not supported", propertyKind));
            }
        }

        public string PropertyName { get; private set; }
        public PropertyKind Kind { get; private set; }
        public SingleColumnBinding SingleBinding { get; private set; }
        public UnionReferencesBinding UnionBinding { get; private set; }
    }
}