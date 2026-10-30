using System;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Client.Hosting;

namespace saiflow.engine.Client;

/// <summary>
/// 进程内共享的 <see cref="SailClient"/> 入口（非业务 Cash）。
/// 宿主 Assemble / Builder.Build 后调用 <see cref="Use"/>；同时绑定 <see cref="SailSqlAccess"/> 供 Module 使用。
/// </summary>
public static class SailClientAccess
{
    private static SailClient _client;

    public static bool IsConfigured => _client != null;

    public static void Use(SailClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        SailSqlAccess.Use(() => _client.UseRunSQL());
    }

    public static void Clear()
    {
        _client = null;
        SailSqlAccess.Clear();
    }

    public static SailClient Required
        => _client ?? throw new InvalidOperationException(
            "SailClient is not bound. Call SailClientAccess.Use(client) or SailClient.CreateBuilder()....Build() when assembling the host.");

    /// <summary>
    /// 若已绑定则返回；否则尝试懒构建（需先 <see cref="SailClientHost.Configure"/>）。
    /// </summary>
    public static SailClient Ensure()
        => IsConfigured ? Required : SailClientHost.GetOrCreate();

    /// <summary>每请求独立的运行库 SQLBuilder。</summary>
    public static SQLBuilder UseRunSQL() => Required.UseRunSQL();
}
