using System;
using sailflow.action.Hosting;
using sailflow.engine.Hosting;

namespace sailflow.biz.Hosting;

/// <summary>
/// Biz composition：启用 Action 扩展（IronPython）。
/// </summary>
public static class BizActionSailExtensions
{
    /// <summary>
    /// 注册 Python Action/Script 处理器；之后仍可用 <c>UseActionMethod</c> / <c>UseScriptMethod</c> 扩展。
    /// </summary>
    public static ISailCollection UseBizAction(this ISailCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.UseIronPython();
    }

    public static SailClientBuilder UseBizAction(this SailClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseIronPython();
    }
}
