/* Simple JSON serializer (public domain) used for small projects. */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public static class MiniJSON
{
    public static string Serialize(object obj)
    {
        var sb = new StringBuilder();
        SerializeValue(obj, sb);
        return sb.ToString();
    }

    public static object Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        // Attempt a simple parse using Unity's JsonUtility into a Dictionary wrapper
        try
        {
            var dict = UnityEngine.JsonUtility.FromJson<Wrapper>("{\"json\":\"\"}");
        }
        catch { }
        // Fallback: try to parse limited JSON - we will implement a naive parser for flat objects/arrays
        return SimpleJsonParser.Parse(json);
    }

    static void SerializeValue(object value, StringBuilder sb)
    {
        if (value == null) { sb.Append("null"); return; }
        if (value is string) { sb.Append('"'); sb.Append(Escape((string)value)); sb.Append('"'); return; }
        if (value is bool) { sb.Append((bool)value ? "true" : "false"); return; }
        if (value is IList)
        {
            sb.Append('[');
            bool first = true;
            foreach (var v in (IList)value)
            {
                if (!first) sb.Append(','); first = false;
                SerializeValue(v, sb);
            }
            sb.Append(']');
            return;
        }
        if (value is IDictionary)
        {
            sb.Append('{');
            bool first = true;
            foreach (DictionaryEntry e in (IDictionary)value)
            {
                if (!first) sb.Append(','); first = false;
                sb.Append('"'); sb.Append(Escape(e.Key.ToString())); sb.Append('"'); sb.Append(':');
                SerializeValue(e.Value, sb);
            }
            sb.Append('}');
            return;
        }
        if (value is double || value is float || value is int || value is long || value is decimal)
        {
            sb.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
            return;
        }
        // fallback
        sb.Append('"'); sb.Append(Escape(value.ToString())); sb.Append('"');
    }

    static string Escape(string a)
    {
        return a.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    [Serializable]
    #pragma warning disable 0649 // Field is never assigned (used only for JsonUtility deserialization)
    private class Wrapper { public string json; }
    #pragma warning restore 0649

    // Very small JSON parser for our limited needs (flat object with string/number values)
    static class SimpleJsonParser
    {
        public static object Parse(string s)
        {
            s = s.Trim();
            if (s.StartsWith("{")) return ParseObject(s);
            if (s.StartsWith("[")) return ParseArray(s);
            return s;
        }

        static object ParseArray(string s)
        {
            var list = new List<object>();
            // very naive split
            s = s.Trim(); s = s.Substring(1, s.Length - 2);
            var parts = SplitTopLevel(s);
            foreach (var p in parts) list.Add(Parse(p));
            return list;
        }

        static object ParseObject(string s)
        {
            var dict = new Dictionary<string, object>();
            s = s.Trim(); s = s.Substring(1, s.Length - 2);
            var parts = SplitTopLevel(s);
            foreach (var p in parts)
            {
                var idx = p.IndexOf(':');
                if (idx < 0) continue;
                var k = p.Substring(0, idx).Trim().Trim('"');
                var v = p.Substring(idx + 1).Trim();
                if (v.StartsWith("\"")) dict[k] = v.Trim('"');
                else if (v == "null") dict[k] = null;
                else if (v == "true" || v == "false") dict[k] = v == "true";
                else if (v.StartsWith("{")) dict[k] = ParseObject(v);
                else if (v.StartsWith("[")) dict[k] = ParseArray(v);
                else if (v.Contains(".")) { if (double.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d)) dict[k] = d; }
                else if (long.TryParse(v, out var l)) dict[k] = l;
                else dict[k] = v;
            }
            return dict;
        }

        static List<string> SplitTopLevel(string s)
        {
            var parts = new List<string>();
            int depth = 0; int start = 0; bool inStr = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\') { i++; continue; }
                if (c == '"') inStr = !inStr;
                if (inStr) continue;
                if (c == '{' || c == '[') depth++;
                else if (c == '}' || c == ']') depth--;
                else if (c == ',' && depth == 0)
                {
                    parts.Add(s.Substring(start, i - start)); start = i + 1;
                }
            }
            if (start < s.Length) parts.Add(s.Substring(start));
            for (int i = 0; i < parts.Count; i++) parts[i] = parts[i].Trim();
            return parts;
        }
    }
}
