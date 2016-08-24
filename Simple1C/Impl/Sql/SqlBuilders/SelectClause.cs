using System.Collections.Generic;
using System.Text;

namespace Simple1C.Impl.Sql.SqlBuilders
{
    public class SelectClause
    {
        public List<SelectField> Fields { get; set; }
        public List<JoinClause> JoinClauses { get; set; }
        public string TableName { get; set; }
        public string TableAlias { get; set; }

        public string GetSql()
        {
            var b = new StringBuilder();
            b.Append("select");
            var isFirst = true;
            foreach (var f in Fields)
            {
                if (isFirst)
                    isFirst = false;
                else
                    b.Append(",");
                b.Append("\r\n\t");
                SqlHelpers.WriteReference(b, f.TableName, f.Name);
                SqlHelpers.WriteAlias(b, f.Alias);
            }
            b.Append("\r\nfrom ");
            SqlHelpers.WriteDeclaration(b, TableName, TableAlias);
            foreach (var join in JoinClauses)
            {
                b.Append("\r\n");
                b.Append(join.JoinKind);
                b.Append(" join ");
                SqlHelpers.WriteDeclaration(b, join.TableName, join.TableAlias);
                b.Append(" on ");
                SqlHelpers.WriteReference(b, join.LeftFieldTableName, join.LeftFieldName);
                b.Append(" = ");
                SqlHelpers.WriteReference(b, join.RightFieldTableName, join.RightFieldName);
            }
            return b.ToString();
        }
    }
}