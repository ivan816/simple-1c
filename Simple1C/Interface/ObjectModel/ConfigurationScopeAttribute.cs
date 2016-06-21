using System;

namespace Simple1C.Interface.ObjectModel
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