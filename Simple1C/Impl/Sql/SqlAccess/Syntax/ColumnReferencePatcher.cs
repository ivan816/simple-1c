using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class ColumnReferencePatcher : SqlVisitor
    {
        private DeclarationClause currentDeclaration;

        private readonly Dictionary<string, DeclarationClause> nameToDeclaration =
            new Dictionary<string, DeclarationClause>();

        public override ISqlElement VisitDeclaration(DeclarationClause clause)
        {
            currentDeclaration = clause;
            nameToDeclaration.Add(clause.GetRefName(), clause);
            return base.VisitDeclaration(clause);
        }

        public override ISqlElement VisitColumnReference(ColumnReferenceExpression expression)
        {
            var items = expression.Name.Split('.');
            var aliasCandidate = items[0];
            DeclarationClause table;
            if (nameToDeclaration.TryGetValue(aliasCandidate, out table))
            {
                expression.Name = items.Skip(1).JoinStrings(".");
                expression.TableName = aliasCandidate;
            }
            else
                expression.TableName = currentDeclaration.GetRefName();
            return expression;
        }
    }
}