using System;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;

namespace TransposingMatrix
{
    /// <summary>
    /// Turns the values of a result into text, the same on every server whatever its language:
    /// ISO dates with the column's own precision, and numbers with a decimal point.
    /// </summary>
    internal sealed class ValueFormatter
    {
        private readonly string[] _types;
        private readonly int[] _scales;

        public ValueFormatter(SqlDataReader reader)
        {
            _types = new string[reader.FieldCount];
            _scales = new int[reader.FieldCount];
            var schema = reader.GetSchemaTable();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                _types[i] = reader.GetDataTypeName(i).ToLowerInvariant();
                var scale = schema?.Rows[i]["NumericScale"];
                _scales[i] = scale is short s ? s : 7;
            }
        }

        public string? Format(SqlDataReader reader, int i)
        {
            var value = reader.GetValue(i);
            switch (value)
            {
                case DBNull _:
                    return null;
                case string text:
                    return text;
                case DateTime date:
                    return date.ToString(DateTimeFormat(_types[i], _scales[i]), CultureInfo.InvariantCulture);
                case DateTimeOffset offset:
                    return offset.ToString("yyyy-MM-dd HH:mm:ss" + Fraction(_scales[i]) + " zzz", CultureInfo.InvariantCulture);
                case TimeSpan time:
                    return time.ToString(@"hh\:mm\:ss" + (_scales[i] > 0 ? @"\." + new string('f', _scales[i]) : ""), CultureInfo.InvariantCulture);
                case byte[] bytes:
                    return ByteArrayToString(bytes);
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    // bit, and the CLR types: geography and geometry as WKT, hierarchyid as a path
                    return value.ToString();
            }
        }

        private static string DateTimeFormat(string type, int scale)
        {
            switch (type)
            {
                case "date":
                    return "yyyy-MM-dd";
                case "smalldatetime":
                    return "yyyy-MM-dd HH:mm:ss";
                case "datetime2":
                    return "yyyy-MM-dd HH:mm:ss" + Fraction(scale);
                default:
                    // datetime, and a datetime in a sql_variant
                    return "yyyy-MM-dd HH:mm:ss.fff";
            }
        }

        private static string Fraction(int scale) => scale > 0 ? "." + new string('f', scale) : "";

        public static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(2 + ba.Length * 2).Append("0x");
            foreach (var b in ba)
                hex.Append(b.ToString("X2", CultureInfo.InvariantCulture));
            return hex.ToString();
        }
    }
}
