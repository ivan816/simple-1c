using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess.Parsing;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Impl.Sql.Translation.QueryEntities;
using Simple1C.Impl.Sql.Translation.Visitors;

namespace Simple1C.Impl.Sql.Translation
{
    internal class QueryToSqlTranslator
    {
        private readonly QueryParser queryParser;
        private readonly IMappingSource mappingSource;
        private readonly List<ISqlElement> areas;

        public QueryToSqlTranslator(IMappingSource mappingSource, int[] areas)
        {
            queryParser = new QueryParser();
            this.mappingSource = mappingSource;
            if (areas.Length > 0)
                this.areas = areas.Select(x => new LiteralExpression {Value = x})
                    .Cast<ISqlElement>()
                    .ToList();
        }

        public DateTime? CurrentDate { private get; set; }

        public string Translate(string source)
        {
            var queryEntityRegistry = new QueryEntityRegistry(mappingSource);
            var queryEntityAccessor = new QueryEntityAccessor(queryEntityRegistry);
            var nameGenerator = new NameGenerator();
            var currentDateString = FormatDateTime(CurrentDate ?? DateTime.Today);
            source = nowMacroRegex.Replace(source, currentDateString);
            var selectClause = queryParser.Parse(source);

            RewriteSqlQuery(selectClause, queryEntityRegistry, queryEntityAccessor, nameGenerator);
            return SqlFormatter.Format(selectClause);
        }

        private void RewriteSqlQuery(SqlQuery sqlQuery, QueryEntityRegistry queryEntityRegistry, QueryEntityAccessor queryEntityAccessor, NameGenerator nameGenerator)
        {
            TableDeclarationVisitor.Visit(sqlQuery, clause =>
            {
                queryEntityRegistry.RegisterTable(clause);
                return clause;
            });
            SubqueryVisitor.Visit(sqlQuery, clause =>
            {
                queryEntityRegistry.RegisterSubquery(clause);
                return clause;
            });

            new AddAreaToJoinConditionVisitor().Visit(sqlQuery);

            new ColumnReferenceRewriter(queryEntityAccessor).Visit(sqlQuery);

            var tableDeclarationRewriter = new TableDeclarationRewriter(queryEntityRegistry,
                queryEntityAccessor, nameGenerator, areas);
            tableDeclarationRewriter.RewriteTables(sqlQuery);

            new ValueLiteralRewriter(queryEntityAccessor, queryEntityRegistry).Visit(sqlQuery);

            new QueryFunctionRewriter().Visit(sqlQuery);
        }

        private static string FormatDateTime(DateTime dateTime)
        {
            return string.Format("ДАТАВРЕМЯ({0:yyyy},{0:MM},{0:dd})", dateTime);
        }

        private static readonly Regex nowMacroRegex = new Regex(@"&Now",
         RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
    }
}