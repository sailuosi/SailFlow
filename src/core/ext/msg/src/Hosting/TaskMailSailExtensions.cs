using System;
using sailflow.engine.Client;
using sailflow.engine.Hosting;
using sailflow.engine.Hooks;
using sailflow.msg.Notify;

namespace sailflow.msg.Hosting
{
    /// <summary>默认待办邮件 Notifier 注册；SMTP 选项在 Use 时刻传入。</summary>
    public static class TaskMailSailExtensions
    {
        public static ISailCollection UseDefaultTaskMail(
            this ISailCollection services,
            Action<TaskMailOptions> configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            var options = new TaskMailOptions();
            configure?.Invoke(options);

            return services.AddFeature(new SailFeatureDescriptor
            {
                Name = SailFeatureNames.DefaultTaskMail,
                Phase = SailFeaturePhase.ConfigureClient,
                Order = 110,
                DependsOn = new[] { SailFeatureNames.Json },
                ConfigureClient = (client, _) =>
                {
                    client.addNotifier(new DefaultMailTaskNotifier(client, options), new HookRegistrationOptions
                    {
                        Match = HookMatch.All,
                        TaskKinds = new[] { TaskNotifyKind.Created, TaskNotifyKind.Completed },
                        Mode = HookMode.AsyncOutbox,
                    });
                }
            });
        }

        public static SailClientBuilder UseDefaultTaskMail(
            this SailClientBuilder builder,
            Action<TaskMailOptions> configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.UseDefaultTaskMail(configure);
            return builder;
        }

        public static SailClient UseDefaultTaskMail(
            this SailClient client,
            Action<TaskMailOptions> configure = null)
        {
            ArgumentNullException.ThrowIfNull(client);
            var options = new TaskMailOptions();
            configure?.Invoke(options);
            client.addNotifier(new DefaultMailTaskNotifier(client, options), new HookRegistrationOptions
            {
                Match = HookMatch.All,
                TaskKinds = new[] { TaskNotifyKind.Created, TaskNotifyKind.Completed },
                Mode = HookMode.AsyncOutbox,
            });
            return client;
        }
    }
}
