using System;
using System.Linq.Expressions;

namespace Simple1C.Impl.Queriables
{
    internal class SelectedProperty
    {
        public Expression expression;
        public Func<object[], object> compiledExpression;
        public bool needLocalEval;
        public SelectedPropertyItem[] items;
    }
}