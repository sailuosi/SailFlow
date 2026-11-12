using System;

namespace sailflow.engine.Client;

/// <summary>
/// 组装 <see cref="SailClient"/> 骨架。具体 <see cref="SailDBFactory"/> 由调用方注入（应用宿主构建）。
/// </summary>
public static class SailClientAssembler
{
    /// <summary>
    /// 组装骨架：挂载 DBFactory、Dialect（数据库工厂 + 事件）、默认 <see cref="SailConfig"/>。
    /// 配置形状请随后 <c>useConfig</c> / Builder <c>UseConfig</c> 登记，不在此解析配置源。
    /// </summary>
    public static SailClient Assemble(
        SailDBFactory dbFactory,
        Action<SailClient> configure = null)
    {
        ArgumentNullException.ThrowIfNull(dbFactory);

        var client = new SailClient
        {
            DBFactory = dbFactory,
            Dialect = null!,
            Config = null!
        };

        client.Dialect = new SailDialect(client);
        client.Config = new SailConfig(client);

        client.EnsurePorts();
        configure?.Invoke(client);

        AssertSkeletonReady(client);
        return client;
    }

    /// <summary>使用主库 <paramref name="db"/> 组装（等价于 <see cref="DelegateSailDBFactory"/>）。</summary>
    public static SailClient Assemble(
        mooSQL.data.DBInstance db,
        int mainPosition = 0,
        Action<SailClient> configure = null)
    {
        ArgumentNullException.ThrowIfNull(db);
        return Assemble(new DelegateSailDBFactory(db, mainPosition), configure);
    }

    /// <summary>使用 <c>Func&lt;int, DBInstance&gt;</c> 组装。</summary>
    public static SailClient Assemble(
        Func<int, mooSQL.data.DBInstance> getDb,
        int mainPosition = 0,
        Action<SailClient> configure = null)
    {
        ArgumentNullException.ThrowIfNull(getDb);
        return Assemble(new DelegateSailDBFactory(getDb, mainPosition), configure);
    }

    public static void AssertSkeletonReady(SailClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        Require(client.DBFactory != null, nameof(client.DBFactory));
        Require(client.Dialect != null, nameof(client.Dialect));
        Require(client.Config != null, nameof(client.Config));
        Require(client.Dialect.DbFactory != null, "Dialect.DbFactory");
        Require(client.Dialect.Event != null, "Dialect.Event");
    }

    private static void Require(bool condition, string name)
    {
        if (!condition)
            throw new InvalidOperationException($"SailClient skeleton missing: {name}");
    }
}
