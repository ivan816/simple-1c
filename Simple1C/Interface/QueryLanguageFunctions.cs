using System;
using System.Globalization;

namespace Simple1C.Interface
{
    public static class QueryLanguageFunctions
    {
        private static readonly CultureInfo russianCultureInfo = CultureInfo.GetCultureInfo("ru-RU");

        public static string Presentation(object obj)
        {
            if (obj == null)
                return "";
            var objType = obj.GetType();
            if (typeof(int).IsAssignableFrom(objType))
                return ((int) obj).ToString(russianCultureInfo);
            if (typeof(byte).IsAssignableFrom(objType))
                return ((byte) obj).ToString(russianCultureInfo);
            if (typeof(sbyte).IsAssignableFrom(objType))
                return ((sbyte) obj).ToString(russianCultureInfo);
            if (typeof(short).IsAssignableFrom(objType))
                return ((short) obj).ToString(russianCultureInfo);
            if (typeof(ushort).IsAssignableFrom(objType))
                return ((ushort) obj).ToString(russianCultureInfo);
            if (typeof(uint).IsAssignableFrom(objType))
                return ((uint)obj).ToString(russianCultureInfo);
            if (typeof(long).IsAssignableFrom(objType))
                return ((long)obj).ToString(russianCultureInfo);
            if (typeof(ulong).IsAssignableFrom(objType))
                return ((ulong)obj).ToString(russianCultureInfo);
            if (typeof(float).IsAssignableFrom(objType))
                return ((float)obj).ToString(russianCultureInfo);
            if (typeof(double).IsAssignableFrom(objType))
                return ((double)obj).ToString(russianCultureInfo);
            if (typeof(decimal).IsAssignableFrom(objType))
                return ((decimal)obj).ToString(russianCultureInfo);
            if (typeof(bool).IsAssignableFrom(objType))
                return (bool)obj ? "Да": "Нет";
            if (typeof(string).IsAssignableFrom(objType))
                return (string)obj;
            if (typeof(Guid).IsAssignableFrom(objType))
                return ((Guid)obj).ToString();
            throw new NotSupportedException();
        }
    }
}