using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class ColumnReferencePatcher : SqlVisitor
    {
        private TableDeclarationClause currentTableDeclaration;

        private readonly Dictionary<string, TableDeclarationClause> nameToDeclaration =
            new Dictionary<string, TableDeclarationClause>();

        public override void VisitTableDeclaration(TableDeclarationClause clause)
        {
            currentTableDeclaration = clause;
            nameToDeclaration.Add(clause.GetRefName(), clause);
        }

        public override void VisitColumnReference(ColumnReferenceExpression expression)
        {
            var items = expression.Name.Split('.');
            var aliasCandidate = items[0];
            TableDeclarationClause table;
            if (nameToDeclaration.TryGetValue(aliasCandidate, out table))
            {
                expression.Name = items.Skip(1).JoinStrings(".");
                expression.TableName = aliasCandidate;
            }
            else
                expression.TableName = currentTableDeclaration.GetRefName();
        }
    }
}