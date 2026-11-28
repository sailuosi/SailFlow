using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sailflow.utils
{
    /// <summary>
    /// <c>Task.RowPara</c> 查询串工具：<c>key=val&amp;key2=val2</c>；值经 URL 编码。
    /// </summary>
    public static class RowParaHelper
    {
        public const int MaxLength = 1000;
        public const string RemindWayKey = "remindWay";

        public static IDictionary<string, string> Parse(string rowPara)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(rowPara))
                return map;

            foreach (var part in rowPara.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var i = part.IndexOf('=');
                if (i <= 0)
                    continue;
                var key = Uri.UnescapeDataString(part[..i].Trim());
                if (string.IsNullOrEmpty(key))
                    continue;
                var val = i + 1 < part.Length ? Uri.UnescapeDataString(part[(i + 1)..]) : "";
                map[key] = val;
            }
            return map;
        }

        public static string Format(IDictionary<string, string> map)
        {
            if (map == null || map.Count == 0)
                return null;

            var sb = new StringBuilder();
            foreach (var kv in map.Where(x => !string.IsNullOrWhiteSpace(x.Key)))
            {
                if (sb.Length > 0)
                    sb.Append('&');
                sb.Append(Uri.EscapeDataString(kv.Key.Trim()));
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(kv.Value ?? ""));
            }
            return Truncate(sb.ToString(), MaxLength);
        }

        /// <summary>合并：同名键以 <paramref name="overlay"/> 为准；结果限长 <see cref="MaxLength"/>。</summary>
        public static string Merge(string baseRowPara, string overlayRowPara)
        {
            var map = Parse(baseRowPara);
            foreach (var kv in Parse(overlayRowPara))
                map[kv.Key] = kv.Value;
            return Format(map);
        }

        public static string Merge(string baseRowPara, IDictionary<string, string> overlay)
        {
            var map = Parse(baseRowPara);
            if (overlay != null)
            {
                foreach (var kv in overlay)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key))
                        continue;
                    map[kv.Key] = kv.Value ?? "";
                }
            }
            return Format(map);
        }

        public static string SetKey(string rowPara, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Truncate(rowPara, MaxLength);
            var map = Parse(rowPara);
            if (value == null)
                map.Remove(key);
            else
                map[key] = value;
            return Format(map);
        }

        public static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max)
                return string.IsNullOrEmpty(s) ? null : s;
            return s[..max];
        }

        public static string TruncateTips(string tips) => Truncate(tips, 500);
    }
}
