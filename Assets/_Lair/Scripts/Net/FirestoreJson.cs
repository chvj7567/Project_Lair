using System.Text;
using System.Text.RegularExpressions;

namespace Lair.Net
{
    //# Firestore REST 타입 JSON(stringValue/integerValue) 빌드·파싱 헬퍼. HTTP 는 CHMHttpNetwork 담당.
    public static class FirestoreJson
    {
        public static string StringField(string value) => "{\"stringValue\":\"" + Escape(value) + "\"}";

        public static string IntField(long value) => "{\"integerValue\":\"" + value + "\"}";

        public static string Document(params (string name, string valueJson)[] fields)
        {
            StringBuilder sb = new StringBuilder("{\"fields\":{");
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                sb.Append('"').Append(fields[i].name).Append("\":").Append(fields[i].valueJson);
            }
            sb.Append("}}");
            return sb.ToString();
        }

        //# "fieldName":{"stringValue":"..."} 패턴에서 값 추출. 없으면 null.
        public static string ExtractString(string documentJson, string fieldName)
        {
            Match m = Regex.Match(documentJson ?? string.Empty,
                "\"" + Regex.Escape(fieldName) + "\"\\s*:\\s*\\{\\s*\"stringValue\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            return m.Success ? Unescape(m.Groups[1].Value) : null;
        }

        public static long ExtractInt(string documentJson, string fieldName)
        {
            Match m = Regex.Match(documentJson ?? string.Empty,
                "\"" + Regex.Escape(fieldName) + "\"\\s*:\\s*\\{\\s*\"integerValue\"\\s*:\\s*\"?(-?\\d+)\"?");
            return m.Success && long.TryParse(m.Groups[1].Value, out long v) ? v : 0;
        }

        public static string ExtractUpdateTime(string documentJson)
        {
            Match m = Regex.Match(documentJson ?? string.Empty, "\"updateTime\"\\s*:\\s*\"([^\"]+)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string Escape(string s) => (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        private static string Unescape(string s) => (s ?? string.Empty).Replace("\\\"", "\"").Replace("\\\\", "\\");
    }
}
