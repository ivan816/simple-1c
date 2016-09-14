using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    internal class ColumnReferenceTableNameRewriter : SqlVisitor
    {
        private readonly Dictionary<string, TableDeclarationClause> nameToDeclaration =
            new Dictionary<string, TableDeclarationClause>(StringComparer.OrdinalIgnoreCase);

        //мудотня какая-то, разобрать
        //без алиасов соответствие колонка->таблица можно
        //построить только зная какие колонки есть в каждой таблице =>
        //должно быть где-то выше, где уже есть TableMapping
        private TableDeclarationClause currentTableDeclaration;

        //todo remove copypaste
        public override SelectClause VisitSelect(SelectClause clause)
        {
            nameToDeclaration.Clear();
            clause.Source = Visit(clause.Source);
            VisitEnumerable(clause.JoinClauses);
            if (clause.Fields != null)
                VisitEnumerable(clause.Fields);
            if (clause.WhereExpression != null)
                clause.WhereExpression = VisitWhere(clause.WhereExpression);
            if (clause.GroupBy != null)
                clause.GroupBy = VisitGroupBy(clause.GroupBy);
            if (clause.Union != null)
                clause.Union = VisitUnion(clause.Union);
            if (clause.OrderBy != null)
                clause.OrderBy = VisitOrderBy(clause.OrderBy);
            return clause;
        }

        public override ISqlElement VisitTableDeclaration(TableDeclarationClause clause)
        {
            currentTableDeclaration = clause;
            nameToDeclaration.Add(clause.GetRefName(), clause);
            return clause;
        }

        public override ISqlElement VisitColumnReference(ColumnReferenceExpression expression)
        {
            var items = expression.Name.Split('.');
            var possiblyAlias = items[0];
            TableDeclarationClause table;
            if (nameToDeclaration.TryGetValue(possiblyAlias, out table))
            {
                expression.Name = items.Skip(1).JoinStrings(".");
                expression.Declaration = table;
            }
            else
                expression.Declaration = currentTableDeclaration;
            return expression;
        }
    }
}