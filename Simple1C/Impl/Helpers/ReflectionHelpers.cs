using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Simple1C.Impl.Helpers.MemberAccessor;

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

        private static readonly ConcurrentDictionary<MethodBase, Func<object, object[], object>> compiledMethods =
            new ConcurrentDictionary<MethodBase, Func<object, object[], object>>();

        private static readonly Func<MethodBase, Func<object, object[], object>> compileMethodDelegate =
            EmitCallOf;

        public static Func<object, object[], object> GetCompiledDelegate(MethodBase targetMethod)
        {
            return compiledMethods.GetOrAdd(targetMethod, compileMethodDelegate);
        }

        public static Func<object, object[], object> EmitCallOf(MethodBase targetMethod)
        {
            var dynamicMethod = new DynamicMethod("",
                                                  typeof(object),
                                                  new[] { typeof(object), typeof(object[]) },
                                                  typeof(ReflectionHelpers),
                                                  true);
            var il = dynamicMethod.GetILGenerator();
            if (!targetMethod.IsStatic && !targetMethod.IsConstructor)
            {
                il.Emit(OpCodes.Ldarg_0);
                if (targetMethod.DeclaringType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, targetMethod.DeclaringType);
                    il.DeclareLocal(targetMethod.DeclaringType);
                    il.Emit(OpCodes.Stloc_0);
                    il.Emit(OpCodes.Ldloca_S, 0);
                }
                else
                    il.Emit(OpCodes.Castclass, targetMethod.DeclaringType);
            }
            var parameters = targetMethod.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                if (i <= 8)
                    il.Emit(ToConstant(i));
                else
                    il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);
                var unboxingCaster = new UnboxingCaster(typeof(object), parameters[i].ParameterType);
                unboxingCaster.EmitCast(il);
            }
            Type returnType;
            if (targetMethod.IsConstructor)
            {
                var constructorInfo = (ConstructorInfo)targetMethod;
                returnType = constructorInfo.DeclaringType;
                il.Emit(OpCodes.Newobj, constructorInfo);
            }
            else
            {
                var methodInfo = (MethodInfo)targetMethod;
                returnType = methodInfo.ReturnType;
                il.Emit(dynamicMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt, methodInfo);
            }
            if (returnType == typeof(void))
                il.Emit(OpCodes.Ldnull);
            else
            {
                var resultCaster = new BoxingCaster(typeof(object), returnType);
                resultCaster.EmitCast(il);
            }
            il.Emit(OpCodes.Ret);
            return (Func<object, object[], object>)dynamicMethod.CreateDelegate(typeof(Func<object, object[], object>));
        }

        private static OpCode ToConstant(int i)
        {
            switch (i)
            {
                case 0:
                    return OpCodes.Ldc_I4_0;
                case 1:
                    return OpCodes.Ldc_I4_1;
                case 2:
                    return OpCodes.Ldc_I4_2;
                case 3:
                    return OpCodes.Ldc_I4_3;
                case 4:
                    return OpCodes.Ldc_I4_4;
                case 5:
                    return OpCodes.Ldc_I4_5;
                case 6:
                    return OpCodes.Ldc_I4_6;
                case 7:
                    return OpCodes.Ldc_I4_7;
                case 8:
                    return OpCodes.Ldc_I4_8;
                default:
                    throw new InvalidOperationException("method can't have more than 9 parameters");
            }
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