using System;
using saiflow.engine.Hosting;
using saiflow.engine.Hooks;

namespace saiflow.biz.Hosting;

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
