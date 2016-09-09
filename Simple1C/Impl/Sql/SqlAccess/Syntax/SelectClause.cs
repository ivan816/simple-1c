using System.Collections.Generic;
using System.Linq;
using Irony.Ast;
using Irony.Parsing;
using Simple1C.Impl.Sql.SqlAccess.Parsing;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class SelectClause : ISqlElement, IAstNodeInit
    {
        public SelectClause()
        {
            JoinClauses = new List<JoinClause>();
            Columns = new List<SelectColumn>();
        }

        public List<SelectColumn> Columns { get; private set; }
        public List<JoinClause> JoinClauses { get; private set; }
        public ISqlElement WhereExpression { get; set; }
        public DeclarationClause Table { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitSelect(this);
        }

        public void Init(AstContext context, ParseTreeNode parseNode)
        {
            var elements = parseNode.Elements();
            Columns = elements.OfType<SelectColumn>().ToList();
            Table = elements.OfType<DeclarationClause>().Single();
            foreach (var c in Columns)
                ((ColumnReferenceExpression) c.Expression).TableName = Table.Name;
        }
    }
}