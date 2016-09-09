using System.Collections.Generic;
using Irony.Parsing;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    internal static class ParseHelpers
    {
        public static List<object> Elements(this ParseTreeNode n)
        {
            var result = new List<object>();
            foreach (var node in n.ChildNodes)
            {
                var holder = node.AstNode as ElementsHolder;
                if (holder != null)
                {
                    result.AddRange(holder.Elements);
                    continue;
                }
                if (node.AstNode != null)
                    result.Add(node.AstNode);
            }
            return result;
        }
    }
}