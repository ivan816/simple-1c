using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Threading;
using Simple1C.Interface.Sql;

namespace Simple1C.Impl.Sql
{
    internal class Query1CReader : IQuery1CReader
    {
        private DataColumn[] columns;
        private object[] record;

        public Query1CReader()
        {
            Queue = new ConcurrentQueue<object[]>();
            Errors = new ConcurrentBag<Exception>();
        }

        public Thread[] ExecutionThreads { get; set; }
        public ConcurrentQueue<object[]> Queue { get; private set; }
        public ConcurrentBag<Exception> Errors { get; private set; }

        public DataColumn[] Columns
        {
            get
            {
                if (!WaitFor(() => HasColumns))
                {
                    const string msg = "all threads exited, but no column information provided";
                    throw new InvalidOperationException(msg);
                }

                lock (this)
                    return columns;
            }
            set
            {
                lock (this)
                {
                    if (columns != null)
                        return;
                    columns = value;
                }
            }
        }

        public bool Read()
        {
            return WaitFor(() => !Queue.IsEmpty) && Queue.TryDequeue(out record);
        }

        public object[] GetValues()
        {
            return record;
        }

        private bool HasColumns
        {
            get
            {
                lock (this)
                {
                    return columns != null;
                }
            }
        }

        private bool WaitFor(Func<bool> predicate)
        {
            do
            {
                if (Errors.Count > 0)
                    throw Errors.First();
                if (predicate())
                    return true;
                if (ExecutionThreads.Length == 0)
                    return false;
                ExecutionThreads = ExecutionThreads
                    .Where(t => !t.Join(10))
                    .ToArray();
            } while (true);
        }
    }
}