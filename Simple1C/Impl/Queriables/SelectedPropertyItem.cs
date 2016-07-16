using System.Runtime.CompilerServices;

namespace Simple1C.Impl.Queriables
{
    internal struct SelectedPropertyItem
    {
        public SelectedPropertyItem(object constant, int queryFieldIndex)
        {
            this.constant = constant;
            this.queryFieldIndex = queryFieldIndex;
        }

        private readonly object constant;
        private readonly int queryFieldIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetValue(object[] fieldValues)
        {
            return queryFieldIndex < 0 ? constant : fieldValues[queryFieldIndex];
        }
    }
}