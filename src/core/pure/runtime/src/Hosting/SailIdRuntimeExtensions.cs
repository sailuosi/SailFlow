using System;
using sailflow.engine.Client;
using sailflow.engine.Hosting;
using sailflow.runtime.Ids;

namespace sailflow.runtime.Hosting;

/// <summary>runtime 内置 Id 实现注册（GuidString + DefaultBpmnCodec）。</summary>
public static class SailIdRuntimeExtensions
{
    /// <summary>挂载 Guid D 发号 + 默认 BPMN codec。</summary>
    public static SailIdBuilder UseGuidString(this SailIdBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        var src = GuidStringIdSource.Instance;
        return builder
            .UseSource(src)
            .UseBpmnCodec(new DefaultSailBpmnIdCodec(src));
    }

    /// <summary>仅注册内置 Guid 发号（不含 NextStep 等其它 runtime 默认）。</summary>
    public static SailClientBuilder UseSailIdDefaults(this SailClientBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        builder.Services.UseSailIds(o => o.UseGuidString());
        return builder;
    }

    public static ISailCollection UseSailIdDefaults(this ISailCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        return services.UseSailIds(o => o.UseGuidString());
    }

    public static SailClient UseSailIdDefaults(this SailClient client)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        return client.UseSailIds(o => o.UseGuidString());
    }
}
