using System;
using sailflow.engine.Hosting;
using sailflow.hub.Hosting;
using sailflow.hub.Localize;

namespace sailflow.biz.Hosting;

/// <summary>
/// Biz composition helper: enable JSON localization (re-exports UseLocalize).
/// </summary>
public static class LocalizeBizSailExtensions
{
    /// <summary>
    /// Registers sailflow.localize JSON messages. Prefer calling early (before Form/Resource/AI).
    /// 默认中文；语言也可用 Builder.<c>UseLang</c> 显式配置。
    /// </summary>
    public static ISailCollection UseBizLocalize(this ISailCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.UseLocalize();
    }

    /// <summary>
    /// Registers JSON messages and sets language in one call.
    /// </summary>
    public static ISailCollection UseBizLocalize(this ISailCollection services, LangTypeEnum lang)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.UseLocalize();
        services.UseLang(lang);
        return services;
    }
}
