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
        private const int postgreSqlMaxAliasLength = 31;
        public const string queryHints = "SET enable_nestloop = off;";

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
            var currentDateString = FormatDateTime(CurrentDate ?? DateTime.Today);
            source = nowMacroRegex.Replace(source, currentDateString);
            var sqlQuery = queryParser.Parse(source);
            foreach (var unionClause in sqlQuery.Unions)
                SetDefaultAliases(unionClause.SelectClause);
            var translationContext = new TranslationContext(mappingSource, areas, sqlQuery);
            translationContext.Execute();
            return queryHints + "\r\n" + SqlFormatter.Format(sqlQuery);
        }

        private static void SetDefaultAliases(SelectClause selectClause)
        {
            var used = new HashSet<string>();
            if (selectClause.Fields == null)
                return;
            foreach (var f in selectClause.Fields)
            {
                if (string.IsNullOrEmpty(f.Alias))
                {
                    var columnReference = f.Expression as ColumnReferenceExpression;
                    if (columnReference != null)
                        f.Alias = columnReference.Name.Replace('.', '_');
                    else
                        continue;
                }
                const int lengthThreshold = postgreSqlMaxAliasLength - 2;
                if (f.Alias.Length > lengthThreshold)
                    f.Alias = f.Alias.Substring(f.Alias.Length - lengthThreshold, lengthThreshold);
                var s = f.Alias;
                var index = 1;
                while (!used.Add(f.Alias))
                {
                    if (index == 9)
                    {
                        const string messageFormat = "too many fields with same prefix [{0}]";
                        throw new InvalidOperationException(string.Format(messageFormat, s));
                    }
                    f.Alias = s + '_' + ++index;
                }
            }
        }

        private static string FormatDateTime(DateTime dateTime)
        {
            return dateTime.Hour == 0 && dateTime.Minute == 0 && dateTime.Second == 0
                ? string.Format("ДАТАВРЕМЯ({0:yyyy},{0:MM},{0:dd})", dateTime)
                : string.Format("ДАТАВРЕМЯ({0:yyyy},{0:MM},{0:dd},{0:HH},{0:mm},{0:ss})", dateTime);
        }

        private static readonly Regex nowMacroRegex = new Regex(@"&Now",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private class TranslationContext
        {
            private readonly IMappingSource mappingSource;
            private readonly List<ISqlElement> areas;
            private readonly SqlQuery sqlQuery;
            private readonly NameGenerator nameGenerator = new NameGenerator();
            private readonly QueryEntityRegistry queryEntityRegistry;
            private readonly QueryEntityTree queryEntityTree;
            private readonly EnumSqlBuilder enumSqlBuilder;

            public TranslationContext(IMappingSource mappingSource, List<ISqlElement> areas,
                SqlQuery sqlQuery)
            {
                this.mappingSource = mappingSource;
                this.areas = areas;
                this.sqlQuery = sqlQuery;
                queryEntityRegistry = new QueryEntityRegistry(mappingSource);
                queryEntityTree = new QueryEntityTree(queryEntityRegistry, nameGenerator);
                enumSqlBuilder = new EnumSqlBuilder(queryEntityTree, nameGenerator);
            }

            public void Execute()
            {
                new ObjectNameCheckingVisitor(mappingSource).Visit(sqlQuery);
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
                new DeduceEntityTypeFromIsReferenceExpressionVisitor(queryEntityTree)
                    .Visit(sqlQuery);
                var rewrittenColumns = new HashSet<ColumnReferenceExpression>();
                new IsReferenceExpressionRewriter(queryEntityTree, nameGenerator, rewrittenColumns)
                    .Visit(sqlQuery);
                new ColumnReferenceRewriter(queryEntityTree, rewrittenColumns, nameGenerator)
                    .Visit(sqlQuery);
                new TableDeclarationRewriter(queryEntityTree, enumSqlBuilder, nameGenerator, areas)
                    .RewriteTables(sqlQuery);
                new ValueLiteralRewriter(enumSqlBuilder).Visit(sqlQuery);
                new QueryFunctionRewriter().Visit(sqlQuery);
            }
        }
    }
}