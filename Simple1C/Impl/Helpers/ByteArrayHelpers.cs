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

        //http://stackoverflow.com/questions/623104/byte-to-hex-string/623184#623184
        public static byte[] FromHex(string hex, int offset = 0)
        {
            var bytes = new byte[(hex.Length - offset) / 2];
            for (int bx = 0, sx = 0; bx < bytes.Length; ++bx, ++sx)
            {
                var c = hex[sx + offset];
                bytes[bx] = (byte)((c > '9' ? (c > 'Z' ? c - 'a' + 10 : c - 'A' + 10) : c - '0') << 4);
                c = hex[++sx + offset];
                bytes[bx] |= (byte)(c > '9' ? (c > 'Z' ? c - 'a' + 10 : c - 'A' + 10) : c - '0');
            }
            return bytes;
        }
    }
}