using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Simple1C.Impl;
using Simple1C.Impl.Helpers;
using Simple1C.Impl.Helpers.MemberAccessor;

namespace Simple1C.Interface
{
    public static class Функции
    {
        private static readonly ConcurrentDictionary<string, object> accessors = new ConcurrentDictionary<string, object>();
        private static readonly CultureInfo russianCultureInfo = CultureInfo.GetCultureInfo("ru-RU");

        public static string Представление(object obj)
        {
            //TODO NullabeTypes
            if (obj == null)
                return "";
            var objType = obj.GetType();
            var stringObj = obj as string;
            if (stringObj != null)
                return stringObj;
            if (objType.IsClass)
            {
                var configurationName = ConfigurationName.Get(objType);
                if (configurationName.Scope == ConfigurationScope.Справочники)
                    return GetPropertyValue<string>(objType, "Наименование", obj) ?? "";
                if (configurationName.Scope == ConfigurationScope.Документы)
                {
                    var builder = new StringBuilder();
                    builder.Append(Synonym.OfClass(obj));
                    var number = GetPropertyValue<string>(objType, "Номер", obj);
                    if (!string.IsNullOrEmpty(number))
                    {
                        builder.Append(" ");
                        builder.Append(number);
                    }
                    var date = GetPropertyValue<DateTime?>(objType, "Дата", obj);
                    if (date.HasValue)
                    {
                        builder.Append(" от ");
                        builder.Append(date.Value.ToString("dd.MM.yyyy HH:mm:ss"));
                    }
                    return builder.ToString();
                }
                if (configurationName.Scope == ConfigurationScope.ПланыСчетов)
                    return GetPropertyValue<string>(objType, "Код", obj) ?? "";
            }
            if (objType.IsEnum)
                return Synonym.OfEnumUnsafe(obj);
            if (obj is int)
                return ((int) obj).ToString(russianCultureInfo);
            if (obj is long)
                return ((long)obj).ToString(russianCultureInfo);
            if (obj is decimal)
                return ((decimal)obj).ToString(russianCultureInfo);
            if (obj is bool)
                return (bool)obj ? "Да" : "Нет";
            if (obj is Guid)
                return ((Guid)obj).ToString();
            if (obj is byte)
                return ((byte) obj).ToString(russianCultureInfo);
            if (obj is sbyte)
                return ((sbyte) obj).ToString(russianCultureInfo);
            if (obj is short)
                return ((short) obj).ToString(russianCultureInfo);
            if (obj is ushort)
                return ((ushort) obj).ToString(russianCultureInfo);
            if (obj is uint)
                return ((uint)obj).ToString(russianCultureInfo);
            if (obj is ulong)
                return ((ulong)obj).ToString(russianCultureInfo);
            if (obj is float)
                return ((float)obj).ToString(russianCultureInfo);
            if (obj is double)
                return ((double)obj).ToString(russianCultureInfo);
            const string messageFormat = "can't get ПРЕДСТАВЛЕНИЕ for object [{0}] of type [{1}]";
            throw new NotSupportedException(string.Format(messageFormat, obj, objType.FormatName()));
        }

        private static T GetPropertyValue<T>(Type objType, string propertyName, object obj)
        {
            var key = objType.FullName + "_" + propertyName;
            object accessor;
            MemberAccessor<T> typedAccessor;
            if (!accessors.TryGetValue(key, out accessor))
            {
                var property = objType.GetProperty("Дата");
                typedAccessor = property != null ? MemberAccessor<T>.Get(property) : null;
                accessors.TryAdd(key, typedAccessor);
            }
            else
                typedAccessor = (MemberAccessor<T>) accessor;
            return typedAccessor == null ? default(T) : typedAccessor.Get(obj);
        }
    }
}