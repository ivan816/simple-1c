using System.Collections.Generic;
using Irony.Ast;
using Irony.Parsing;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    internal class ElementsHolder : IAstNodeInit
    {
        public List<object> Elements { get; private set; }

        public void Init(AstContext context, ParseTreeNode treeNode)
        {
            Elements = treeNode.Elements();
        }
    }
}