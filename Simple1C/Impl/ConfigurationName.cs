using System;
using System.Collections.Concurrent;
using System.Reflection;
using Simple1C.Impl.Helpers;
using Simple1C.Interface;
using Simple1C.Interface.ObjectModel;

namespace Simple1C.Impl
{
    internal struct ConfigurationName : IEquatable<ConfigurationName>
    {
        private static readonly ConcurrentDictionary<Type, ConfigurationName?> cache =
            new ConcurrentDictionary<Type, ConfigurationName?>();

        private static Func<Type, ConfigurationName?> createAttributes;
        private readonly ConfigurationScope scope;
        private readonly string name;

        public ConfigurationName(ConfigurationScope scope, string name)
        {
            this.scope = scope;
            this.name = name;
        }

        public ConfigurationScope Scope
        {
            get { return scope; }
        }

        public bool HasReference
        {
            get
            {
                return scope == ConfigurationScope.Документы ||
                       scope == ConfigurationScope.Справочники ||
                       scope == ConfigurationScope.ПланыСчетов ||
                       scope == ConfigurationScope.ПланыВидовХарактеристик;
            }
        }

        public string Name
        {
            get { return name; }
        }

        public static ConfigurationName? GetOrNull(Type type)
        {
            if (createAttributes == null)
                createAttributes = CreateName;
            return cache.GetOrAdd(type, createAttributes);
        }

        public static ConfigurationName Get(Type type)
        {
            var result = GetOrNull(type);
            if (!result.HasValue)
            {
                const string messageFormat = "can't get [ConfigurationName] for [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, type.FormatName()));
            }
            return result.Value;
        }

        public static ConfigurationName Parse(string s)
        {
            var result = ParseOrNull(s);
            if (!result.HasValue)
            {
                const string messageFormat = "can't parse [{0}] from [{1}]";
                throw new InvalidOperationException(string.Format(messageFormat,
                    typeof(ConfigurationName).FormatName(), s));
            }
            return result.Value;
        }

        public static ConfigurationName? ParseOrNull(string s)
        {
            var items = s.Split('.');
            if (items.Length != 2)
                return null;
            var scope = ParseScopeNameOrNull(items[0]);
            return scope.HasValue
                ? new ConfigurationName(scope.Value, items[1])
                : (ConfigurationName?) null;
        }

        public string Fullname
        {
            get { return GetScopeName() + "." + name; }
        }

        public ConfigurationName ChangeName(string newName)
        {
            return new ConfigurationName(scope, newName);
        }

        private string GetScopeName()
        {
            switch (scope)
            {
                case ConfigurationScope.Справочники:
                    return "Справочник";
                case ConfigurationScope.Документы:
                    return "Документ";
                case ConfigurationScope.РегистрыСведений:
                    return "РегистрСведений";
                case ConfigurationScope.РегистрыБухгалтерии:
                    return "РегистрБухгалтерии";
                case ConfigurationScope.Перечисления:
                    return "Перечисление";
                case ConfigurationScope.ПланыСчетов:
                    return "ПланСчетов";
                case ConfigurationScope.Константы:
                    return "Константа";
                case ConfigurationScope.ПланыВидовХарактеристик:
                    return "ПланВидовХарактеристик";
                default:
                    const string messageFormat = "unexpected scope [{0}]";
                    throw new InvalidOperationException(string.Format(messageFormat, scope));
            }
        }

        private static ConfigurationScope? ParseScopeNameOrNull(string s)
        {
            switch (s.ToLower())
            {
                case "справочник":
                    return ConfigurationScope.Справочники;
                case "документ":
                    return ConfigurationScope.Документы;
                case "регистрсведений":
                    return ConfigurationScope.РегистрыСведений;
                case "регистрбухгалтерии":
                    return ConfigurationScope.РегистрыБухгалтерии;
                case "перечисление":
                    return ConfigurationScope.Перечисления;
                case "плансчетов":
                    return ConfigurationScope.ПланыСчетов;
                case "константа":
                    return ConfigurationScope.Константы;
                case "планвидовхарактеристик":
                    return ConfigurationScope.ПланыВидовХарактеристик;
                default:
                    return null;
            }
        }

        public bool Equals(ConfigurationName other)
        {
            return scope == other.scope && string.Equals(name, other.name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ConfigurationName && Equals((ConfigurationName) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) scope*397) ^ name.GetHashCode();
            }
        }

        public static bool operator ==(ConfigurationName left, ConfigurationName right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ConfigurationName left, ConfigurationName right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return Fullname;
        }

        private static ConfigurationName? CreateName(Type type)
        {
            if (type.IsEnum)
                return new ConfigurationName(ConfigurationScope.Перечисления, type.Name);
            var attributes = (ConfigurationScopeAttribute[])
                type.GetCustomAttributes<ConfigurationScopeAttribute>(true);
            if (attributes.Length == 0)
                return null;
            if (attributes.Length > 1)
            {
                const string messageFormat = "more than one [ConfigurationScopeAttribute]'s for type: [{0}]";
                throw new InvalidOperationException(string.Format(messageFormat, type.FormatName()));
            }
            return new ConfigurationName(attributes[0].Scope, type.Name);
        }
    }
}