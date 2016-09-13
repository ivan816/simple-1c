using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SchemaMapping;
using Simple1C.Impl.Sql.SqlAccess;
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
                {"датавремя", "datetime"},
                {"год", "year"},
                {"квартал", "quarter"},
                {"значение", "value"}
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

        public DateTime? CurrentDate { get; set; }

        public string Translate(string source)
        {
            var currentDateString = FormatDateTime(CurrentDate ?? DateTime.Today);
            source = nowMacroRegex.Replace(source, currentDateString);
            source = keywordsRegex.Replace(source, m => keywordsMap[m.Groups[1].Value]);
            var queryParser = new QueryParser();
            var selectClause = queryParser.Parse(source);
            var translationVisitor = new TranslationVisitor(mappingSource, areas);
            translationVisitor.Visit(selectClause);
            return SqlFormatter.Format(selectClause);
        }

        private static string FormatDateTime(DateTime dateTime)
        {
            return string.Format("ДАТАВРЕМЯ({0:yyyy},{0:MM},{0:dd})", dateTime);
        }

        private class TranslationVisitor : SqlVisitor
        {
            private readonly IMappingSource mappingSource;
            private readonly List<ISqlElement> areas;

            public TranslationVisitor(IMappingSource mappingSource, List<ISqlElement> areas)
            {
                this.mappingSource = mappingSource;
                this.areas = areas;
            }

            public override SelectClause VisitSelect(SelectClause selectClause)
            {
                var result = base.VisitSelect(selectClause);

                var queryEntityRegistry = new QueryEntityRegistry(mappingSource);
                var queryEntityAccessor = new QueryEntityAccessor(queryEntityRegistry);
                var tableDeclarationRewriter = new TableDeclarationRewriter(queryEntityRegistry,
                    queryEntityAccessor, areas);

                TableDeclarationVisitor.Visit(selectClause, clause =>
                {
                    queryEntityRegistry.Register(clause);
                    return clause;
                });

                var addAreaToJoinConditionVisitor = new AddAreaToJoinConditionVisitor();
                addAreaToJoinConditionVisitor.Visit(selectClause);

                var referencePatcher = new ColumnReferenceRewriter(queryEntityAccessor);
                referencePatcher.Visit(selectClause);

                TableDeclarationVisitor.Visit(selectClause, tableDeclarationRewriter.Rewrite);

                var valueLiteralRewriter = new ValueLiteralRewriter(queryEntityAccessor, queryEntityRegistry);
                valueLiteralRewriter.Visit(selectClause);

                var queryFunctionRewriter = new QueryFunctionRewriter();
                queryFunctionRewriter.Visit(selectClause);

                return result;
            }
        }
    }
}