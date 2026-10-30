using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using mooSQL.data;

namespace saiflow.data
{
    /// <summary>
    /// 业务侧用 <see cref="IDictionary{TKey,TValue}"/> / 匿名对象传参，转成 mooSQL <see cref="Paras"/>。
    /// </summary>
    public static class SailParamConverter
    {
        public static Paras ToParas(object param)
        {
            var ps = new Paras();
            if (param == null)
                return ps;
            if (param is Paras already)
                return already;

            if (param is IDictionary<string, object> dict)
            {
                foreach (var kv in dict)
                    ps.Add(EnsureAt(kv.Key), kv.Value);
                return ps;
            }

            if (param is IDictionary map)
            {
                foreach (DictionaryEntry entry in map)
                    ps.Add(EnsureAt(Convert.ToString(entry.Key)), entry.Value);
                return ps;
            }

            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(param))
                ps.Add(EnsureAt(prop.Name), prop.GetValue(param));
            return ps;
        }

        private static string EnsureAt(string key)
        {
            if (string.IsNullOrEmpty(key))
                return key;
            return key.StartsWith("@", StringComparison.Ordinal) ? key : "@" + key;
        }
    }
}
