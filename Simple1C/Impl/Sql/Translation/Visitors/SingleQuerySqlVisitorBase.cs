using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    //why? kill.
    internal abstract class SingleQuerySqlVisitorBase : SqlVisitor
    {
        private bool selectVisited;

        public override SqlQuery VisitSqlQuery(SqlQuery clause)
        {
            if (selectVisited)
                return clause;
            selectVisited = true;
            return base.VisitSqlQuery(clause);
        }
    }
}