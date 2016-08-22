using System;
using System.Collections;
using System.Linq;
using Remotion.Linq.Parsing.ExpressionVisitors.Transformation;
using Remotion.Linq.Parsing.Structure;
using Remotion.Linq.Parsing.Structure.NodeTypeProviders;
using Simple1C.Impl.Queriables;

namespace Simple1C.Impl.Helpers
{
    internal static class RelinqHelpers
    {
        private static IQueryParser queryParser;

        public static IQueryProvider CreateQueryProvider(TypeRegistry typeRegistry,
            Func<BuiltQuery, IEnumerable> execute)
        {
            if (queryParser == null)
                queryParser = CreateQueryParser();
            return new RelinqQueryProvider(queryParser,
                new RelinqQueryExecutor(typeRegistry, execute));
        }

        private static IQueryParser CreateQueryParser()
        {
            var nodeTypeProvider = new CompoundNodeTypeProvider(new INodeTypeProvider[]
            {
                MethodInfoBasedNodeTypeRegistry.CreateFromRelinqAssembly()
            });
            var transformerRegistry = ExpressionTransformerRegistry.CreateDefault();
            var expressionTreeParser = new ExpressionTreeParser(nodeTypeProvider,
                ExpressionTreeParser.CreateDefaultProcessor(transformerRegistry));
            return new QueryParser(expressionTreeParser);
        }
    }
}