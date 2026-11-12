using System;
using sailflow.engine.Hosting;
using sailflow.hub.Hosting;

namespace sailflow.biz.Hosting;

/// <summary>
/// Biz composition helper: enable JSON localization (re-exports UseLocalize).
/// </summary>
public static class LocalizeBizSailExtensions
{
    /// <summary>
    /// Registers sailflow.localize JSON messages. Prefer calling early (before Form/Resource/AI).
    /// </summary>
    public static ISailCollection UseBizLocalize(this ISailCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.UseLocalize();
    }
}
