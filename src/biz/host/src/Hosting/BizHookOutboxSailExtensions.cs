using System;
using sailflow.hosting;
using sailflow.hooks;

namespace sailflow.biz.Hosting;

public static class BizHookOutboxSailExtensions
{
    public static ISailCollection UseBizHookOutboxDrain(
        this ISailCollection services,
        Action<HookOutboxOptions> configure = null,
        bool autoStart = false) =>
        services.UseHookOutboxDrain(configure, autoStart);

    public static SailClientBuilder UseBizHookOutboxDrain(
        this SailClientBuilder builder,
        Action<HookOutboxOptions> configure = null,
        bool autoStart = false) =>
        builder.UseHookOutboxDrain(configure, autoStart);
}
