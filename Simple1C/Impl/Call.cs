using System;
using Simple1C.Impl.Com;

namespace Simple1C.Impl
{
    internal static class Call
    {
        public static string ПолноеИмя(object comObject)
        {
            return Convert.ToString(ComHelpers.Invoke(comObject, "ПолноеИмя"));
        }

        public static string Имя(object comObject)
        {
            return Convert.ToString(ComHelpers.GetProperty(comObject, "Имя"));
        }

        public static string Синоним(object comObject)
        {
            return Convert.ToString(ComHelpers.GetProperty(comObject, "Синоним"));
        }

        public static object Получить(object comObject, int index)
        {
            return ComHelpers.Invoke(comObject, "Получить", index);
        }

        public static int Количество(object comObject)
        {
            return Convert.ToInt32(ComHelpers.Invoke(comObject, "Количество"));
        }

        public static bool IsEmpty(object comObject)
        {
            return (bool) ComHelpers.Invoke(comObject, "IsEmpty");
        }

        public static object НайтиПоТипу(object metadata, object typeObject)
        {
            return ComHelpers.Invoke(metadata, "НайтиПоТипу", typeObject);
        }
    }
}