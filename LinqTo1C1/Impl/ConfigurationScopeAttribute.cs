using System;

namespace LinqTo1C.Impl
{
    public class ConfigurationScopeAttribute: Attribute
    {
        public ConfigurationScopeAttribute(ConfigurationScope scope)
        {
            Scope = scope;
        }

        public ConfigurationScope Scope { get; private set; }
    }
}