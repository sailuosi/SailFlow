using System;
using saiflow.engine.Client;

namespace saiflow.engine.Client.Hosting;

/// <summary>
/// 引擎配置形状注册（类比 mooSQL <c>useDataBase(List&lt;DBPosition&gt;)</c>：只登记形状，不解析来源）。
/// </summary>
public static class SailConfigExtensions
{
    /// <summary>在服务集合上登记配置形状；Build 时合并到 <see cref="SailClient.Config"/>。</summary>
    public static ISailCollection UseConfig(this ISailCollection services, SailConfig config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);
        services.Properties[SailBuilderKeys.Config] = config;
        return services;
    }

    /// <summary>登记对默认 <see cref="SailConfig"/> 的修改委托（Build 时对已创建实例执行）。</summary>
    public static ISailCollection UseConfig(this ISailCollection services, Action<SailConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        services.Properties[SailBuilderKeys.ConfigAction] = configure;
        return services;
    }
}
