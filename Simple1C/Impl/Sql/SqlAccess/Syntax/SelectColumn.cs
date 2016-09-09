using System.Linq;
using Irony.Ast;
using Irony.Parsing;
using Simple1C.Impl.Sql.SqlAccess.Parsing;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class SelectColumn : ISqlElement, IAstNodeInit
    {
        public ISqlElement Expression { get; set; }
        public string Alias { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitSelectColumn(this);
        }

        public void Init(AstContext context, ParseTreeNode parseNode)
        {
            Expression = new ColumnReferenceExpression
            {
                Name = parseNode.Elements().OfType<Identifier>().Single().Value
            };
        }
    }
}