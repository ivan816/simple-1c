using Simple1C.Impl.Com;

namespace Simple1C.Impl.Queries
{
    internal class ValueTableColumn : DispatchObject
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