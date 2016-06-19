namespace Simple1C.Impl.Generation
{
    internal static class GeneratorTemplates
    {
        public static readonly SimpleFormat classFormat = SimpleFormat.Parse(@"
using System;
using System.Collections.Generic;
using Simple1C.Interface;
using Simple1C.Interface.ObjectModel;

namespace %namespace-name%
{
    [ConfigurationScope(ConfigurationScope.%configuration-scope%)]
    public partial class %class-name% : Abstract1CEntity
    {
%content%
    }
}");
        public static readonly SimpleFormat nestedClassFormat = SimpleFormat.Parse(@"
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
using System;

namespace %namespace-name%
{
    public enum %name%
    {
%content%
    }
}");

    }
}