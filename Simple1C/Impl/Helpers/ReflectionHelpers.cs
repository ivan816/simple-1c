using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Simple1C.Impl.Helpers
{
    internal static class ReflectionHelpers
    {
        public static string FormatName(this Type type)
        {
            string result;
            if (typeNames.TryGetValue(type, out result))
                return result;
            if (type.IsArray)
                return type.GetElementType().FormatName() + "[]";
            if (type.IsDelegate() && type.IsNested)
                return type.DeclaringType.FormatName() + "." + type.Name;

            if (!type.IsNested || !type.DeclaringType.IsGenericType || type.IsGenericParameter)
                return FormatGenericType(type, type.GetGenericArguments());

            var declaringHierarchy = DeclaringHierarchy(type)
                .TakeWhile(t => t.IsGenericType)
                .Reverse();

            var knownGenericArguments = type.GetGenericTypeDefinition().GetGenericArguments()
                .Zip(type.GetGenericArguments(), (definition, closed) => new {definition, closed})
                .ToDictionary(x => x.definition.GenericParameterPosition, x => x.closed);

            var hierarchyNames = new List<string>();

            foreach (var t in declaringHierarchy)
            {
                var tArguments = t.GetGenericTypeDefinition()
                    .GetGenericArguments()
                    .Where(x => knownGenericArguments.ContainsKey(x.GenericParameterPosition))
                    .ToArray();

                hierarchyNames.Add(FormatGenericType(t,
                    tArguments.Select(x => knownGenericArguments[x.GenericParameterPosition]).ToArray()));

                foreach (var tArgument in tArguments)
                    knownGenericArguments.Remove(tArgument.GenericParameterPosition);
            }
            return string.Join(".", hierarchyNames.ToArray());
        }

        private static IEnumerable<Type> DeclaringHierarchy(Type type)
        {
            yield return type;
            while (type.DeclaringType != null)
            {
                yield return type.DeclaringType;
                type = type.DeclaringType;
            }
        }

        public static bool IsDelegate(this Type type)
        {
            return type.BaseType == typeof (MulticastDelegate);
        }

        private static string FormatGenericType(Type type, Type[] arguments)
        {
            var genericMarkerIndex = type.Name.IndexOf("`", StringComparison.InvariantCulture);
            return genericMarkerIndex > 0
                ? string.Format("{0}<{1}>", type.Name.Substring(0, genericMarkerIndex),
                    arguments.Select(FormatName).JoinStrings(","))
                : type.Name;
        }

        private static readonly IDictionary<Type, string> typeNames = new Dictionary<Type, string>
        {
            {typeof (object), "object"},
            {typeof (byte), "byte"},
            {typeof (short), "short"},
            {typeof (ushort), "ushort"},
            {typeof (int), "int"},
            {typeof (uint), "uint"},
            {typeof (long), "long"},
            {typeof (ulong), "ulong"},
            {typeof (double), "double"},
            {typeof (float), "float"},
            {typeof (string), "string"},
            {typeof (bool), "bool"}
        };

        public static IEnumerable<Type> Parents(this Type type)
        {
            var current = type;
            while (current.BaseType != null)
            {
                yield return current.BaseType;
                current = current.BaseType;
            }
        }

        public static Type MemberType(this MemberInfo memberInfo)
        {
            var info = memberInfo as PropertyInfo;
            if (info != null)
                return info.PropertyType;
            var fieldInfo = memberInfo as FieldInfo;
            if (fieldInfo != null) return fieldInfo.FieldType;
            return null;
        }

        public static IEnumerable<T> EnumValues<T>()
            where T : struct
        {
            return Enum.GetValues(typeof (T)).Cast<T>();
        }

        public static bool IsStatic(this MemberInfo memberInfo)
        {
            if (memberInfo == null)
                return false;
            var property = memberInfo as PropertyInfo;
            if (property != null)
                return IsStatic(property.GetGetMethod()) || IsStatic(property.GetSetMethod());
            var field = memberInfo as FieldInfo;
            if (field != null)
                return field.IsStatic;
            var method = memberInfo as MethodBase;
            return method != null && method.IsStatic;
        }

        public static bool IsNullableOf(this Type type1, Type type2)
        {
            return Nullable.GetUnderlyingType(type1) == type2;
        }

        public static bool IsDefined<TAttribute>(this ICustomAttributeProvider type, bool inherit = true)
            where TAttribute : Attribute
        {
            return type.GetCustomAttributes(typeof (TAttribute), inherit).Any();
        }
    }
}