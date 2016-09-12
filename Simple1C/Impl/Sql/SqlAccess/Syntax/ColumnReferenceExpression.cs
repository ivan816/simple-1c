using System.Linq;
using Irony.Ast;
using Irony.Parsing;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class ColumnReferenceExpression : ISqlElement, IAstNodeInit
    {
        public string Name { get; set; }

        //todo Declaration ref?
        public string TableName { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitColumnReference(this);
        }

        public void Init(AstContext context, ParseTreeNode parseNode)
        {
            Name = parseNode.ChildNodes.Select(x => x.Token.ValueString).JoinStrings(".");
        }
    }
}