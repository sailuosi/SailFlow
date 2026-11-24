using System;
using sailflow.engine.Client;
using sailflow.hosting;
using sailflow.log;
using sailflow.msg.Logging;

namespace sailflow.msg.Hosting
{
    /// <summary>
    /// 启用 <see cref="ConsoleSailLog"/> 作为 <see cref="SailClient.Log"/> 实现。
    /// </summary>
    public static class SailConsoleLogExtensions
    {
        /// <summary>
        /// Builder：注册控制台运行时日志。可同时改 <see cref="SailLogOptions"/>（如 MinimumLevel）。
        /// </summary>
        public static SailClientBuilder UseConsoleSailLog(
            this SailClientBuilder builder,
            Action<SailLogOptions> configureLog = null,
            Action<ConsoleSailLogOptions> configureConsole = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.UseConsoleSailLog(configureLog, configureConsole);
            return builder;
        }

        /// <summary>ISailCollection：登记 ConfigureClient 挂载。</summary>
        public static ISailCollection UseConsoleSailLog(
            this ISailCollection services,
            Action<SailLogOptions> configureLog = null,
            Action<ConsoleSailLogOptions> configureConsole = null)
        {
            ArgumentNullException.ThrowIfNull(services);

            var consoleOptions = new ConsoleSailLogOptions();
            configureConsole?.Invoke(consoleOptions);

            return services.AddFeature(new SailFeatureDescriptor
            {
                Name = SailFeatureNames.ConsoleSailLog,
                Phase = SailFeaturePhase.ConfigureClient,
                Order = 15,
                ConfigureClient = (client, _) =>
                {
                    configureLog?.Invoke(client.LogOptions ??= new SailLogOptions());
                    client.useLogger(new ConsoleSailLog(() => client.LogOptions ?? new SailLogOptions(), consoleOptions));
                }
            });
        }

        /// <summary>已构建 Client：立即替换 <see cref="SailClient.Log"/>。</summary>
        public static SailClient useConsoleSailLog(
            this SailClient client,
            Action<SailLogOptions> configureLog = null,
            Action<ConsoleSailLogOptions> configureConsole = null)
        {
            ArgumentNullException.ThrowIfNull(client);
            configureLog?.Invoke(client.LogOptions ??= new SailLogOptions());
            var consoleOptions = new ConsoleSailLogOptions();
            configureConsole?.Invoke(consoleOptions);
            client.useLogger(new ConsoleSailLog(() => client.LogOptions ?? new SailLogOptions(), consoleOptions));
            return client;
        }
    }
}
