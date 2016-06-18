namespace LinqTo1C.Impl.Generation
{
    public static class GeneratorTemplates
    {
        public static readonly SimpleFormat namespaceFormat = SimpleFormat.Parse(@"
using System;
using System.Collections.Generic;
using LinqTo1C.Interface;

namespace %namespace-name%
{
%content%
}");
        public static readonly SimpleFormat classFormat = SimpleFormat.Parse(@"
    public partial class %class-name% : Abstract1CEntity
    {
%content%
    }");

        public static readonly SimpleFormat propertyFormat = SimpleFormat.Parse(@"
        private Requisite<%type%> %field-name%;
        public %type% %property-name%
        {
            get { return Controller.GetValue(ref %field-name%, ""%property-name%""); }
            set { Controller.SetValue(ref %field-name%, ""%property-name%"", value); }
        }");

        public static readonly SimpleFormat enumFormat = SimpleFormat.Parse(@"
    public enum %name%
    {
%content%
    }");
    }
}