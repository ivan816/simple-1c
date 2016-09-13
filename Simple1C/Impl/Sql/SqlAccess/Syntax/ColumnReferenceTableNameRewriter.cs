using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class ColumnReferenceTableNameRewriter : SqlVisitor
    {
        private readonly Dictionary<string, TableDeclarationClause> nameToDeclaration =
            new Dictionary<string, TableDeclarationClause>(StringComparer.OrdinalIgnoreCase);

        //мудотня какая-то, разобрать
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
            var tableRefName = items[0];
            TableDeclarationClause table;
            if (nameToDeclaration.TryGetValue(tableRefName, out table))
            {
                expression.Name = items.Skip(1).JoinStrings(".");
                expression.TableName = tableRefName;
            }
            else
                expression.TableName = currentTableDeclaration.GetRefName();
            return expression;
        }
    }
}