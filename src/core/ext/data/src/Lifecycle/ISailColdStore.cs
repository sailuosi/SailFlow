using System;
using mooSQL.data;
using sailflow.engine.Client;

namespace sailflow.data.Lifecycle
{
    /// <summary>
    /// 冷存储访问抽象。默认同库；配置切冷库后仅改实现，热路径仍用 <see cref="SailClient.UseRunSQL"/>。
    /// </summary>
    public interface ISailColdStore
    {
        /// <summary>热库（运行时）SQL。</summary>
        SQLBuilder UseHotSQL();

        /// <summary>冷库 SQL（可与热库同一连接）。</summary>
        SQLBuilder UseColdSQL();

        /// <summary>热冷是否同一物理库（同库时可同事务迁出）。</summary>
        bool IsSameDatabase { get; }
    }

    /// <summary>默认同库冷存储；<see cref="SailDataLifecycleOptions.ColdStoreConnectionName"/> 预留，解析由宿主自定义实现。</summary>
    public sealed class PrimarySailColdStore : ISailColdStore
    {
        public PrimarySailColdStore(SailClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public SailClient Client { get; }

        public bool IsSameDatabase =>
            Client.DataLifecycleOptions == null
            || Client.DataLifecycleOptions.ColdStoreUsePrimaryDatabase
            || string.IsNullOrWhiteSpace(Client.DataLifecycleOptions.ColdStoreConnectionName);

        public SQLBuilder UseHotSQL() => Client.UseRunSQL();

        public SQLBuilder UseColdSQL()
        {
            // 多连接工厂未接线前始终主库；宿主可替换整份 ISailColdStore。
            return Client.UseRunSQL();
        }
    }
}
