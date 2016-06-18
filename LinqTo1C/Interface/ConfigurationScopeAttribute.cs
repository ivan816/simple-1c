using System;

namespace LinqTo1C.Interface
{
    public class ConfigurationScopeAttribute : Attribute
    {
        public ConfigurationScopeAttribute(ConfigurationScope scope)
        {
            Scope = scope;
        }

        public ConfigurationScope Scope { get; private set; }
    }
}