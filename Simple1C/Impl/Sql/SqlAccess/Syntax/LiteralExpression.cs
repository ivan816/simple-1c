using System;
using System.Linq;
using System.Text;
using Simple1C.Impl.Helpers;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    internal class LiteralExpression : ISqlElement
    {
        public object Value { get; set; }
        public SqlType? SqlType { get; set; }

        public void WriteTo(StringBuilder b)
        {
            var value = SqlType.HasValue ? ApplySqlType(Value, SqlType.Value) : Value;
            b.Append(FormatValueAsString(value));
        }

        private static string FormatValueAsString(object value)
        {
            if (value is string)
                return "'" + value + "'";
            if (value is byte[])
                return "E'\\\\x" + ((byte[]) value).ToHex();
            return value.ToString();
        }

        private static object ApplySqlType(object value, SqlType sqlType)
        {
            switch (sqlType)
            {
                case Syntax.SqlType.ByteArray:
                    var b = value as byte?;
                    if (b.HasValue)
                        return new[] {b.Value};
                    var i = value as int?;
                    if (i.HasValue)
                        return BitConverter.GetBytes(i.Value).Reverse().ToArray();
                    const string messageFormat = "can't convert value [{0}] of type [{1}] to [{2}]";
                    throw new InvalidOperationException(string.Format(messageFormat,
                        value, value == null ? "<null>" : value.GetType().FormatName(),
                        sqlType));
                default:
                    const string message = "unexpected value [{0}] of SqlType";
                    throw new InvalidOperationException(string.Format(message, sqlType));
            }
        }
    }
}