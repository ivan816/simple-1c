using Simple1C.Impl.Sql.SqlAccess;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal abstract class SingleSelectSqlVisitorBase : SqlVisitor
    {
        private bool selectVisited;

        public override SelectClause VisitSelect(SelectClause clause)
        {
            if (selectVisited)
                return clause;
            selectVisited = true;
            return base.VisitSelect(clause);
        }
    }
}