using System;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.Translation.Visitors
{
    internal class TableDeclarationVisitor : SqlVisitor
    {
        private readonly Func<TableDeclarationClause, ISqlElement> visit;

        public static void Visit(ISqlElement selectClause, Func<TableDeclarationClause, ISqlElement> visit)
        {
            var visitor = new TableDeclarationVisitor(visit);
            visitor.Visit(selectClause);
        }

        private TableDeclarationVisitor(Func<TableDeclarationClause, ISqlElement> visit)
        {
            this.visit = visit;
        }

        public override ISqlElement VisitTableDeclaration(TableDeclarationClause clause)
        {
            return visit(clause);
        }
    }
}