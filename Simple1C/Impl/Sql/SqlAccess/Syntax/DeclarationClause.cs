using System.Linq;
using Irony.Ast;
using Irony.Parsing;
using Simple1C.Impl.Sql.SqlAccess.Parsing;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class DeclarationClause : ISqlElement, IAstNodeInit
    {
        public string Name { get; set; }
        public string Alias { get; set; }

        public ISqlElement Accept(SqlVisitor visitor)
        {
            return visitor.VisitDeclaration(this);
        }

        public void Init(AstContext context, ParseTreeNode parseNode)
        {
            
        }
    }
}