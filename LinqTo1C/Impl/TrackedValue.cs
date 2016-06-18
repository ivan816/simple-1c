using System.Collections;

namespace LinqTo1C.Impl
{
    public struct TrackedValue
    {
        public IList originalList;
        public object observedValue;
    }
}