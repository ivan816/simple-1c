using System;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal class SubqueryVisitor : SqlVisitor
    {
        private readonly Func<SubqueryTable, SubqueryTable> visit;

        public static void Visit(ISqlElement element, Func<SubqueryTable, SubqueryTable> visit)
        {
            new SubqueryVisitor(visit).Visit(element);
        }

        private SubqueryVisitor(Func<SubqueryTable, SubqueryTable> visit)
        {
            this.visit = visit;
        }

        public override SubqueryTable VisitSubqueryTable(SubqueryTable subqueryTable)
        {
            return visit(base.VisitSubqueryTable(subqueryTable));
        }
    }
}