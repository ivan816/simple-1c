using System;
using System.Collections.Generic;
using System.Linq;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Impl.Sql.Translation;

namespace Simple1C.Impl.Sql.SqlAccess.Parsing
{
    internal class ColumnReferenceTableNameResolver : SqlVisitor
    {
        private readonly List<Context> contexts = new List<Context>();

        public override SqlQuery VisitSqlQuery(SqlQuery sqlQuery)
        {
            PushContext();
            var result = base.VisitSqlQuery(sqlQuery);
            PopContext();
            return result;
        }

        //todo remove copypaste
        public override SelectClause VisitSelect(SelectClause clause)
        {
            clause.Source = (IColumnSource) Visit(clause.Source);
            VisitEnumerable(clause.JoinClauses);
            if (clause.Fields != null)
                VisitEnumerable(clause.Fields);
            if (clause.WhereExpression != null)
                clause.WhereExpression = VisitWhere(clause.WhereExpression);
            if (clause.GroupBy != null)
                clause.GroupBy = VisitGroupBy(clause.GroupBy);
            if (clause.Having != null)
                clause.Having = Visit(clause.Having);
            return clause;
        }

        public override SubqueryTable VisitSubqueryTable(SubqueryTable clause)
        {
            var result = base.VisitSubqueryTable(clause);
            Register(result.Alias, result);
            return result;
        }

        public override ISqlElement VisitTableDeclaration(TableDeclarationClause clause)
        {
            Register(clause.GetRefName(), clause);
            return base.VisitTableDeclaration(clause);
        }

        public override ISqlElement VisitColumnReference(ColumnReferenceExpression expression)
        {
            var resolvedColumn = ResolveColumn(expression.Name);
            expression.Name = resolvedColumn.LocalName;
            expression.Table = resolvedColumn.Table;
            return base.VisitColumnReference(expression);
        }

        private void PushContext()
        {
            contexts.Add(new Context());
        }

        private void PopContext()
        {
            if (contexts.Count == 0)
                throw new InvalidOperationException("Assertion failure. Context popped too many times");
            contexts.RemoveAt(contexts.Count - 1);
        }

        private void Register(string refName, IColumnSource clause)
        {
            var current = contexts.Last();
            current.LastDeclaration = clause;
            current.TablesByName[refName] = clause;
        }

        private ResolvedColumn ResolveColumn(string name)
        {
            var items = name.Split('.');
            var possiblyAlias = items[0];
            foreach (var tablesByName in Enumerable.Reverse(contexts))
            {
                IColumnSource table;
                if (tablesByName.TablesByName.TryGetValue(possiblyAlias, out table))
                {
                    return new ResolvedColumn
                    {
                        LocalName = items.Skip(1).JoinStrings("."),
                        Table = table
                    };
                }
            }
            var context = contexts.LastOrDefault();
            if (context != null)
                return new ResolvedColumn
                {
                    LocalName = name,
                    Table = context.LastDeclaration
                };
            throw new InvalidOperationException(string.Format("Could not resolve column named {0}", name));
        }

        private class Context
        {
            //мудотня какая-то, разобрать
            //без алиасов соответствие колонка->таблица можно
            //построить только зная какие колонки есть в каждой таблице =>
            //должно быть где-то выше, где уже есть TableMapping
            public IColumnSource LastDeclaration { get; set; }

            public Dictionary<string, IColumnSource> TablesByName { get; private set; }

            public Context()
            {
                TablesByName = new Dictionary<string, IColumnSource>(StringComparer.InvariantCultureIgnoreCase);
            }
        }

        private struct ResolvedColumn
        {
            public IColumnSource Table { get; set; }
            public string LocalName { get; set; }
        }
    }
}