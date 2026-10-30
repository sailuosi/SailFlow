using System;

namespace saiflow.engine.Client;

/// <summary>
/// 主方言：起步插槽为「数据库工厂」「事件」。禁止业务向本类型无限继承；扩展用 Register/Mount。
/// </summary>
public sealed class SailDialect
{
    private readonly SailClient _client;

    public SailDialect(SailClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public SailClient Client => _client;

    /// <summary>插槽：数据库工厂。</summary>
    public SailDBFactory DbFactory { get; private set; }

    /// <summary>插槽：事件。</summary>
    public ISailEventHub Event { get; private set; }

    public void MountDbFactory(SailDBFactory factory)
    {
        DbFactory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public void MountEvent(ISailEventHub eventHub)
    {
        Event = eventHub ?? throw new ArgumentNullException(nameof(eventHub));
    }

    public void RegisterDbFactory(SailDBFactory factory) => MountDbFactory(factory);

    public void RegisterEvent(ISailEventHub eventHub) => MountEvent(eventHub);
}
