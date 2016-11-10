using System.Threading.Tasks;

namespace Simple1C.Interface.Sql
{
    public class QueryExecutionOptions
    {
        public int BatchSize { get; set; }
        public ParallelOptions ParallelOptions { get; set; }
    }
}