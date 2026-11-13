using System;
using mooSQL.data;
using sailflow.engine.Client;

namespace sailflow.biz.Bootstrap
{
    /// <summary>
    /// 建表已迁出 sailflow 包：请在业务宿主实现 InitDb（参考 samples/external SampleSchemaInitializer，
    /// 或 pxxt8 / sleveen-admin 的 SailHostSchemaInitializer）。
    /// </summary>
    [Obsolete("建表归属业务宿主，不在 sailflow 包内。请复制 samples/external/src/Schema/SampleSchemaInitializer.cs 到宿主。")]
    public static class SailSchemaInitializer
    {
        const string Msg =
            "SailSchemaInitializer 已从 sailflow 移除。请在业务系统侧实现建表（见 sailflow samples/external SampleSchemaInitializer）。";

        public static string EnsureCoreTables(SailClient client) => throw new InvalidOperationException(Msg);
        public static string EnsureCoreTables(SQLBuilder kit) => throw new InvalidOperationException(Msg);
        public static void EnsureTaskDetailsView(SQLBuilder kit) => throw new InvalidOperationException(Msg);
        public static string EnsureTables(SailClient client, bool includeAi = true) => throw new InvalidOperationException(Msg);
        public static string EnsureTables(SQLBuilder kit, bool includeAi = true) => throw new InvalidOperationException(Msg);
        public static int EnsureMinimalSeed(SailClient client) => throw new InvalidOperationException(Msg);
        public static int EnsureMinimalSeed(SQLBuilder kit) => throw new InvalidOperationException(Msg);
        public static SailInitDbResult InitDb(SailClient client, bool seed = true, bool includeAi = true, bool registerAiExecutor = true) =>
            throw new InvalidOperationException(Msg);
    }

    public sealed class SailInitDbResult
    {
        public string Progress { get; set; } = string.Empty;
        public int SeededRows { get; set; }
    }
}
