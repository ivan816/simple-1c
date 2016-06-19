using System;
using System.Collections.Generic;
using System.Text;
using Remotion.Linq.Clauses;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Queriables
{
    internal class QueryBuilder
    {
        private readonly Dictionary<string, object> parameters = new Dictionary<string, object>();
        private readonly List<string> whereParts = new List<string>();
        public Ordering[] Orderings { get; set; }
        private Type sourceType;
        private string sourceName;

        public int? Take { get; set; }

        public void AddWherePart(string formatString, params object[] args)
        {
            whereParts.Add(string.Format(formatString, args));
        }

        public string AddParameter(object value)
        {
            var parameterName = "p" + parameters.Count;
            parameters.Add(parameterName, value);
            return parameterName;
        }

        public BuiltQuery Build()
        {
            var resultBuilder = new StringBuilder();
            resultBuilder.Append("ВЫБРАТЬ ");
            if (Take.HasValue)
            {
                resultBuilder.Append("ПЕРВЫЕ ");
                resultBuilder.Append(Take.Value);
                resultBuilder.Append(" ");
            }
            resultBuilder.Append("src.Ссылка ИЗ ");
            resultBuilder.Append(sourceName);
            resultBuilder.Append(" КАК src");
            if (whereParts.Count > 0)
            {
                resultBuilder.Append(" ГДЕ ");
                if (whereParts.Count == 1)
                    resultBuilder.Append(whereParts[0]);
                else
                {
                    resultBuilder.Append("(");
                    resultBuilder.Append(whereParts.JoinStrings(" И "));
                    resultBuilder.Append(")");
                }
            }
            if (Orderings != null)
            {
                resultBuilder.Append(" УПОРЯДОЧИТЬ ПО ");
                var memberAccessBuilder = new MemberAccessBuilder();
                for (int i = 0; i < Orderings.Length; i++)
                {
                    if (i != 0)
                        resultBuilder.Append(',');
                    var ordering = Orderings[i];
                    resultBuilder.Append(memberAccessBuilder.GetMembers(ordering.Expression));
                    if (ordering.OrderingDirection == OrderingDirection.Desc)
                        resultBuilder.Append(" УБЫВ");
                }
            }
            return new BuiltQuery(sourceType, resultBuilder.ToString(), parameters);
        }

        public void SetSource(Type type, string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                sourceName = name;
                sourceType = ConfigurationName.Parse(sourceName).GetTypeOrNull();
            }
            else
            {
                sourceName = ConfigurationName.Get(type).Fullname;
                sourceType = type;
            }
        }
    }
}