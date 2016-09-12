using System;
using System.Linq;
using System.Text;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Sql.SchemaMapping
{
    internal class PropertyMapping
    {
        public PropertyMapping(string propertyName, SingleLayout singleLayout, UnionLayout unionLayout)
        {
            PropertyName = propertyName;
            SingleLayout = singleLayout;
            UnionLayout = unionLayout;
        }

        public string Serialize()
        {
            var b = new StringBuilder();
            b.Append(PropertyName);
            b.Append(" ");
            if (SingleLayout != null)
            {
                b.Append(PropertyLauout.Single);
                b.Append(" ");
                b.Append(SingleLayout.ColumnName);
                b.Append(" ");
                b.Append(SingleLayout.NestedTableName);
            }
            else
            {
                b.Append(PropertyLauout.UnionReferences);
                b.Append(" ");
                b.Append(UnionLayout.TypeColumnName);
                b.Append(" ");
                b.Append(UnionLayout.TableIndexColumnName);
                b.Append(" ");
                b.Append(UnionLayout.ReferenceColumnName);
                foreach (var t in UnionLayout.NestedTables)
                {
                    b.Append(" ");
                    b.Append(t);
                }
            }
            return b.ToString();
        }

        public static PropertyMapping Parse(string s)
        {
            var columnDesc = s.Split(new[] {" "}, StringSplitOptions.None);
            if (columnDesc.Length < 2)
                throw new InvalidOperationException(string.Format("can't parse line [{0}]", s));
            var queryName = columnDesc[0];
            PropertyLauout propertyLauout;
            if (!Enum.TryParse(columnDesc[1], out propertyLauout))
            {
                const string messageFormat = "can't parse [{0}] from [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    typeof(PropertyLauout).FormatName(), s));
            }
            if (propertyLauout == PropertyLauout.Single)
            {
                var nestedTableName = columnDesc.Length >= 4 ? columnDesc[3] : null;
                var singleInfo = new SingleLayout(columnDesc[2], nestedTableName);
                return new PropertyMapping(queryName, singleInfo, null);
            }
            var unionInfo = new UnionLayout(columnDesc[2],
                columnDesc[3], columnDesc[4],
                columnDesc.Skip(5).ToArray());
            return new PropertyMapping(queryName, null, unionInfo);
        }

        public string PropertyName { get; private set; }
        public SingleLayout SingleLayout { get; private set; }
        public UnionLayout UnionLayout { get; private set; }
    }
}