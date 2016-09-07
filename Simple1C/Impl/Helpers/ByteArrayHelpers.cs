using System;

namespace Simple1C.Impl.Helpers
{
    internal static class ByteArrayHelpers
    {
        public static string ToHex(this byte[] bytes)
        {
            var hex = BitConverter.ToString(bytes);
            return hex.Replace("-", "");
        }
    }
}