using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess.Parsing;
using Simple1C.Impl.Sql.SqlAccess.Syntax;
using Simple1C.Impl.Sql.Translation.QueryEntities;
using Simple1C.Impl.Sql.Translation.Visitors;

namespace Simple1C.Impl.Sql.Translation
{
    internal class QueryToSqlTranslator
    {
        private static readonly Regex nowMacroRegex = new Regex(@"&Now",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private static readonly Dictionary<string, string> keywordsMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"выбрать", "select"},
                {"представление", "presentation"},
                {"как", "as"},
                {"из", "from"},
                {"где", "where"},
                {"и", "and"},
                {"или", "or"},
                {"в", "in"},
                {"датавремя", "datetime"},
                {"год", "year"},
                {"квартал", "quarter"},
                {"значение", "value"},
                {"ложь", "false"},
                {"истина", "true"},
                {"упорядочить", "order"},
                {"сгруппировать", "group"},
                {"соединение", "join"},
                {"полное", "full"},
                {"левое", "left"},
                {"правое", "right"},
                {"внешнее", "outer"},
                {"внутреннее", "outer"},
                {"естьnull", "isnull"},
                {"количество", "count"},
                {"сумма", "sum"},
                {"минимум", "min"},
                {"максимум", "max"},
                {"среднее", "avg"},
            };

        private static readonly Regex keywordsRegex = new Regex(string.Format(@"\b({0})\b",
            keywordsMap.Keys.JoinStrings("|")),
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private readonly List<ISqlElement> areas;
        private readonly IMappingSource mappingSource;

        public QueryToSqlTranslator(IMappingSource mappingSource, int[] areas)
        {
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
            source = keywordsRegex.Replace(source, m => keywordsMap[m.Groups[1].Value]);
            var queryParser = new QueryParser();
            var selectClause = queryParser.Parse(source);
            var queryEntityRegistry = new QueryEntityRegistry(mappingSource);
            var queryEntityAccessor = new QueryEntityAccessor(queryEntityRegistry);
            RewriteSqlQuery(selectClause, queryEntityAccessor, queryEntityRegistry);
            return SqlFormatter.Format(selectClause);
        }

        private void RewriteSqlQuery(SqlQuery selectClause, QueryEntityAccessor queryEntityAccessor, QueryEntityRegistry queryEntityRegistry)
        {
            TableDeclarationVisitor.Visit(selectClause, clause =>
            {
                queryEntityRegistry.RegisterTable(clause);
                return clause;
            });
            SubqueryVisitor.Visit(selectClause, clause =>
            {
                queryEntityRegistry.RegisterSubquery(clause);
                return clause;
            });

            new AddAreaToJoinConditionVisitor().Visit(selectClause);

            new ColumnReferenceRewriter(queryEntityAccessor).Visit(selectClause);

            var tableDeclarationRewriter = new TableDeclarationRewriter(queryEntityRegistry, queryEntityAccessor, areas);
            TableDeclarationVisitor.Visit(selectClause, tableDeclarationRewriter.Rewrite);

            new ValueLiteralRewriter(queryEntityAccessor, queryEntityRegistry).Visit(selectClause);

            new QueryFunctionRewriter().Visit(selectClause);
        }

        private static string FormatDateTime(DateTime dateTime)
        {
            return string.Format("ДАТАВРЕМЯ({0:yyyy},{0:MM},{0:dd})", dateTime);
        }
    }
}