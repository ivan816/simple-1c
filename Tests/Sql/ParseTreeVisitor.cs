using System;
using Irony.Parsing;

namespace Simple1C.Tests.Sql
{
    public abstract class ParseTreeVisitor
    {
        public virtual void Visit(ParseTreeNode n)
        {
            foreach (var c in n.ChildNodes)
                Visit(c);
        }

        protected ParseTreeNode GetChildOrNull(ParseTreeNode n, BnfTerm term)
        {
            foreach (var c in n.ChildNodes)
                if (c.Term == term)
                    return c;
            return null;
        }
        
        protected ParseTreeNode GetChild(ParseTreeNode n, BnfTerm term)
        {
            var result = GetChildOrNull(n, term);
            if(result == null)
            {
                const string messageFormat = "can't find [{0}/{1}]";
                throw new InvalidOperationException(string.Format(messageFormat, 
                    n.Term.Name, term.Name));
            }
            return result;
        }
    }
}