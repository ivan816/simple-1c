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
        private readonly Stack<SqlQueryContext> contexts = new Stack<SqlQueryContext>();

        public override SqlQuery VisitSqlQuery(SqlQuery sqlQuery)
        {
            contexts.Push(new SqlQueryContext());
            var result = base.VisitSqlQuery(sqlQuery);
            contexts.Pop();
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

        public override SubqueryTable VisitSubqueryTable(SubqueryTable subqueryTable)
        {
            var result = base.VisitSubqueryTable(subqueryTable);
            contexts.Peek().Register(result.Alias, result);
            return result;
        }

        public override ISqlElement VisitTableDeclaration(TableDeclarationClause clause)
        {
            contexts.Peek().Register(clause.GetRefName(), clause);
            return base.VisitTableDeclaration(clause);
        }

        public override ISqlElement VisitColumnReference(ColumnReferenceExpression expression)
        {
            var items = expression.Name.Split('.');
            var possiblyAlias = items[0];
            IColumnSource table;
            foreach (var context in contexts)
                if (context.TablesByName.TryGetValue(possiblyAlias, out table))
                {
                    expression.Name = items.Skip(1).JoinStrings(".");
                    expression.Table = table;
                    return expression;
                }
            expression.Table = contexts.Peek().LastDeclaration;
            return expression;
        }

        private class SqlQueryContext
        {
            //мудотня какая-то, разобрать
            //без алиасов соответствие колонка->таблица можно
            //построить только зная какие колонки есть в каждой таблице =>
            //должно быть где-то выше, где уже есть TableMapping
            public IColumnSource LastDeclaration { get; set; }

            public Dictionary<string, IColumnSource> TablesByName { get; private set; }

            public SqlQueryContext()
            {
                TablesByName = new Dictionary<string, IColumnSource>(StringComparer.InvariantCultureIgnoreCase);
            }

            public void Register(string refName, IColumnSource clause)
            {
                LastDeclaration = clause;
                TablesByName[refName] = clause;
            }
        }
    }
}