using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Sql.SqlAccess.Syntax;

namespace Simple1C.Impl.Sql
{
    internal class QueryToSqlTranslator
    {
        private readonly ITableMappingSource mappingSource;

        private static readonly Regex tableNameRegex = new Regex(@"(from|join)\s+([^\s]+)\s+as\s+(\S+)",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex fieldsRegex = new Regex(GetFieldsRegex(),
            RegexOptions.Compiled | RegexOptions.Singleline);

        private readonly Dictionary<string, QueryTable> queryTables = new Dictionary<string, QueryTable>();
        private readonly NameGenerator nameGenerator = new NameGenerator();

        public QueryToSqlTranslator(ITableMappingSource mappingSource)
        {
            this.mappingSource = mappingSource;
        }

        public string Translate(string source)
        {
            source = source.Replace('"', '\'');
            var match = tableNameRegex.Match(source);
            while (match.Success)
            {
                var queryName = match.Groups[2].Value;
                var alias = match.Groups[3].Value;
                queryTables.Add(alias,
                    new QueryTable(mappingSource.GetByQueryName(queryName)));
                match = match.NextMatch();
            }
            var result = fieldsRegex.Replace(source, delegate(Match m)
            {
                var properyPath = m.Groups["prop"].Value;
                var properties = properyPath.Split('.');
                if (properties.Length < 2)
                {
                    const string messageFormat = "invalid propery [{0}], alias must be specified";
                    throw new InvalidOperationException(string.Format(messageFormat, properyPath));
                }
                FunctionName? functionName = null;
                if (m.Groups["func"].Success)
                {
                    var functionNameString = m.Groups["func"].Value;
                    if (functionNameString == "ПРЕДСТАВЛЕНИЕ")
                        functionName = FunctionName.Representation;
                    else
                    {
                        const string messageFormat = "unexpected function [{0}] for [{1}]";
                        throw new InvalidOperationException(string.Format(messageFormat,
                            functionNameString, properyPath));
                    }
                }
                return GetFieldName(properties, functionName);
            });
            result = tableNameRegex.Replace(result,
                m => m.Groups[1].Value + " " + GetSql(m.Groups[3].Value));
            return result;
        }

        private QueryTable GetQueryTable(string alias)
        {
            QueryTable mainTable;
            if (!queryTables.TryGetValue(alias, out mainTable))
            {
                const string messageFormat = "can't find query table by alias [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, alias));
            }
            return mainTable;
        }

        private string GetFieldName(string[] properties, FunctionName? functionName)
        {
            var queryTable = GetQueryTable(properties[0]);
            QueryTableField field = null;
            for (var i = 1; i < properties.Length; i++)
            {
                var propertyName = properties[i];
                field = null;
                var isLastProperty = i == properties.Length - 1;
                foreach (var f in queryTable.fields)
                {
                    var found = f.mapping.PropertyName.EqualsIgnoringCase(propertyName) &&
                                (!isLastProperty || f.functionName == functionName);
                    if (found)
                    {
                        field = f;
                        break;
                    }
                }
                if (field == null)
                {
                    field = new QueryTableField
                    {
                        mapping = queryTable.mapping.GetByPropertyName(propertyName),
                        functionName = isLastProperty ? functionName : null
                    };
                    if (!string.IsNullOrEmpty(field.mapping.NestedTableName))
                    {
                        var tableMapping = mappingSource.GetByQueryName(field.mapping.NestedTableName);
                        if (!tableMapping.IsEnum() || functionName != null)
                            field.nestedTable = new QueryTable(tableMapping, nameGenerator.Generate("__nested_table"));
                    }
                    else if (!isLastProperty)
                    {
                        const string messageFormat = "property [{0}] has no table mapping, property path [{1}]";
                        throw new InvalidOperationException(string.Format(messageFormat,
                            propertyName, properties.JoinStrings(".")));
                    }
                    queryTable.fields.Add(field);
                }
                queryTable = field.nestedTable;
            }
            if (field == null)
                throw new InvalidOperationException("assertion failure");
            if (field.alias == null && (properties.Length > 2 || field.nestedTable != null))
                field.alias = nameGenerator.Generate("__nested_field");
            return properties[0] + "." + (field.alias ?? field.mapping.FieldName);
        }

        private string GetSql(string alias)
        {
            var table = GetQueryTable(alias);
            var hasNestedTables = false;
            foreach (var f in table.fields)
                if (f.nestedTable != null)
                {
                    hasNestedTables = true;
                    break;
                }
            string sql;
            if (hasNestedTables)
            {
                var selectClause = new SelectClause(table.mapping.DbTableName, table.alias);
                BuildSubQuery(table, selectClause);
                sql = "(" + selectClause.GetSql() + ")";
            }
            else
                sql = table.mapping.DbTableName;
            return sql + " as " + alias;
        }

        private void BuildSubQuery(QueryTable table, SelectClause target)
        {
            foreach (var f in table.fields)
                AddFieldToSubquery(table, f, target);
        }

        private void AddFieldToSubquery(QueryTable table, QueryTableField field, SelectClause target)
        {
            if (field.nestedTable == null)
            {
                target.Fields.Add(new SelectField
                {
                    Name = field.mapping.FieldName,
                    Alias = field.alias,
                    TableName = table.alias
                });
                return;
            }
            var joinClause = new JoinClause
            {
                TableAlias = field.nestedTable.alias,
                TableName = field.nestedTable.mapping.DbTableName,
                JoinKind = "left",
                EqConditions = new[]
                {
                    new JoinEqCondition
                    {
                        FieldName = field.nestedTable.mapping.GetByPropertyName("Ссылка").FieldName,
                        ComparandFieldName = field.mapping.FieldName,
                        ComparandTableName = table.alias
                    }
                }
            };  
            target.JoinClauses.Add(joinClause);
            if (field.nestedTable.mapping.IsEnum())
            {
                var enumMappingsTableAlias = nameGenerator.Generate("__nested_table");
                var enumMappingsJoinClause = new JoinClause
                {
                    TableName = "simple1c__enumMappings",
                    TableAlias = enumMappingsTableAlias,
                    JoinKind = "left",
                    EqConditions = new[]
                    {
                        new JoinEqCondition
                        {
                            FieldName = "enumName",
                            ComparandConstantValue = "'" + field.nestedTable.mapping.ObjectName + "'"
                        },
                        new JoinEqCondition
                        {
                            FieldName = "orderIndex",
                            ComparandTableName = field.nestedTable.alias,
                            ComparandFieldName = field.nestedTable.mapping.GetByPropertyName("Порядок").FieldName
                        }
                    }
                };
                target.JoinClauses.Add(enumMappingsJoinClause);
                target.Fields.Add(new SelectField
                {
                    Name = "enumValueName",
                    Alias = field.alias,
                    TableName = enumMappingsTableAlias
                });
            }
            else
                BuildSubQuery(field.nestedTable, target);
        }

        private static string GetFieldsRegex()
        {
            const string propRegex = @"[a-zA-Z]+\.[^\,\s]+";
            return string.Format(@"(?<func>ПРЕДСТАВЛЕНИЕ)\((?<prop>{0})\)|(?<prop>{0})",
                propRegex);
        }

        private class QueryTable
        {
            public QueryTable(TableMapping mapping, string alias = null)
            {
                this.mapping = mapping;
                this.alias = alias ?? "__nested_main_table";
            }

            public readonly TableMapping mapping;
            public readonly string alias;
            public readonly List<QueryTableField> fields = new List<QueryTableField>();
        }

        private class QueryTableField
        {
            public PropertyMapping mapping;
            public FunctionName? functionName;
            public string alias;
            public QueryTable nestedTable;
        }

        private enum FunctionName
        {
            Representation
        }

        private class NameGenerator
        {
            private readonly Dictionary<string, int> lastUsed = new Dictionary<string, int>();

            public string Generate(string prefix)
            {
                int lastUsedForPrefix;
                var number = lastUsed[prefix] = lastUsed.TryGetValue(prefix, out lastUsedForPrefix)
                    ? lastUsedForPrefix + 1
                    : 0;
                return prefix + number;
            }
        }
    }
}