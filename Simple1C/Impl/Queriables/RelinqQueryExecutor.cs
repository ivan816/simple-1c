using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Remotion.Linq;

namespace Simple1C.Impl.Queriables
{
    internal class RelinqQueryExecutor : IQueryExecutor
    {
        private readonly TypeRegistry typeRegistry;
        private readonly Func<BuiltQuery, IEnumerable> execute;

        public RelinqQueryExecutor(TypeRegistry typeRegistry, Func<BuiltQuery, IEnumerable> execute)
        {
            this.typeRegistry = typeRegistry;
            this.execute = execute;
        }

        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            return ExecuteCollection<T>(queryModel).Single();
        }

        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            return returnDefaultWhenEmpty
                ? ExecuteCollection<T>(queryModel).SingleOrDefault()
                : ExecuteCollection<T>(queryModel).Single();
        }

        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            BuiltQuery builtQuery;
            if (EntityHelpers.IsConstant(typeof(T)))
                builtQuery = BuiltQuery.Constant(typeof(T));
            else
            {
                var builder = new QueryBuilder(typeRegistry);
                queryModel.Accept(new QueryModelVisitor(builder));
                builtQuery = builder.Build();
            }
            return execute(builtQuery).Cast<T>();
        }
    }
}