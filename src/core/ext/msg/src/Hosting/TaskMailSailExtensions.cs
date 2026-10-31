using System;
using saiflow.engine.Client;
using saiflow.engine.Client.Hosting;
using saiflow.engine.Hooks;
using saiflow.msg.Notify;

namespace saiflow.msg.Hosting
{
    /// <summary>默认待办邮件 Notifier 注册。</summary>
    public static class TaskMailSailExtensions
    {
        public static ISailCollection UseDefaultTaskMail(this ISailCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            return services.AddFeature(new SailFeatureDescriptor
            {
                Name = "saiflow.msg.DefaultMail",
                Phase = SailFeaturePhase.ConfigureClient,
                Order = 110,
                ConfigureClient = (client, _) =>
                {
                    client.useTaskNotifier(new DefaultMailTaskNotifier(client), new HookRegistrationOptions
                    {
                        Match = HookMatch.All,
                        TaskKinds = new[] { TaskNotifyKind.Created, TaskNotifyKind.Completed },
                        Mode = HookMode.AsyncOutbox,
                    });
                }
            });
        }

        public static SailClientBuilder UseDefaultTaskMail(this SailClientBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.UseDefaultTaskMail();
            return builder;
        }

        public static SailClient UseDefaultTaskMail(this SailClient client)
        {
            ArgumentNullException.ThrowIfNull(client);
            client.useTaskNotifier(new DefaultMailTaskNotifier(client), new HookRegistrationOptions
            {
                Match = HookMatch.All,
                TaskKinds = new[] { TaskNotifyKind.Created, TaskNotifyKind.Completed },
                Mode = HookMode.AsyncOutbox,
            });
            return client;
        }
    }
}
