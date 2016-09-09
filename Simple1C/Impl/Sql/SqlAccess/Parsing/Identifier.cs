using System.Linq;
using Irony.Ast;
using Irony.Parsing;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    public class Identifier : IAstNodeInit
    {
        public string Value { get; set; }

        public void Init(AstContext context, ParseTreeNode parseNode)
        {
            Value = parseNode.ChildNodes.Select(x => x.Token.ValueString).JoinStrings(".");
        }
    }
}