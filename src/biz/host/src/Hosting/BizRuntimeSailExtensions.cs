using sailflow.engine.Client;
using sailflow.hosting;
using sailflow.runtime.Hosting;

namespace sailflow.biz.Hosting;

/// <summary>产品门面：挂载 pure 默认扩展（LINQ / Guid 发号 / 内置 Action 方法；见 <see cref="SailRuntimeExtensions.UseSailRuntimeDefaults"/>）。</summary>
public static class BizRuntimeSailExtensions
{
    public static SailClientBuilder UseBizRuntimeDefaults(this SailClientBuilder builder)
        => builder.UseSailRuntimeDefaults();

    public static ISailCollection UseBizRuntimeDefaults(this ISailCollection services)
        => services.UseSailRuntimeDefaults();

    public static SailClient UseBizRuntimeDefaults(this SailClient client)
        => client.UseSailRuntimeDefaults();
}
