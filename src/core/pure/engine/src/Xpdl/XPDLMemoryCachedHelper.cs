using System;
using mooSQL.data;
using saiflow.engine.Utility;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Xpdl
{
    /// <summary>
    /// XPDL / BPMN 流程定义缓存键与读写（经 <see cref="ISooCache"/>，由 <c>SailClient.Cache</c> 提供）。
    /// </summary>
    internal static class XPDLMemoryCachedHelper
    {
        internal static string BuildKey(string processId, string version)
        {
            var str = string.Format("{0}-{1}", processId, version);
            return MD5Helper.GetMD5(str);
        }

        /// <summary>写入流程模型缓存（未指定 TTL 时用 Cache 实现默认过期）。</summary>
        internal static void SetXpdlCache(ISooCache cache, string processId, string version, Process process)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            cache.Add(BuildKey(processId, version), process);
        }

        /// <summary>写入并指定绝对过期秒数。</summary>
        internal static void SetXpdlCache(
            ISooCache cache, string processId, string version, Process process, int cacheDurationInSeconds)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            cache.Add(BuildKey(processId, version), process, cacheDurationInSeconds);
        }

        /// <summary>读取流程模型缓存；未命中返回 null。</summary>
        internal static Process GetXpdlCache(ISooCache cache, string processId, string version)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            return cache.Get<Process>(BuildKey(processId, version));
        }

        /// <summary>已存在则覆盖；不存在返回 false。</summary>
        internal static bool TryUpdate(ISooCache cache, string processId, string version, Process process)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            var key = BuildKey(processId, version);
            if (!cache.ContainsKey(key))
                return false;
            cache.Add(key, process);
            return true;
        }
    }
}
