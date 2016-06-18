using LinqTo1C.Impl.Com;

namespace LinqTo1C.Impl.Queries
{
    public class ValueTableColumn : DispatchObject
    {
        public ValueTableColumn(object comObject)
            : base(comObject)
        {
        }

        public string Name
        {
            get { return GetString("Name"); }
        }
    }
}