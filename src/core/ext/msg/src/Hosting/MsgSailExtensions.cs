using System;
using sailflow.engine.Client;
using sailflow.engine.Hosting;

namespace sailflow.msg.Hosting
{
    /// <summary>
    /// 注册 sailflow.msg RabbitMQ 实现到 SailClient / Builder。
    /// </summary>
    public static class MsgSailExtensions
    {
        public static SailClientBuilder UseRabbitMq(this SailClientBuilder builder, Action<RabbitMqOptions> configure = null)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.Services.UseRabbitMq(configure);
            return builder;
        }

        public static ISailCollection UseRabbitMq(this ISailCollection services, Action<RabbitMqOptions> configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            var options = new RabbitMqOptions();
            configure?.Invoke(options);

            services.ProvideFeature(SailFeatureNames.MessageQueue, order: 20);
            return services.AddFeature(new SailFeatureDescriptor
            {
                Name = SailFeatureNames.RabbitMq,
                Phase = SailFeaturePhase.ConfigureClient,
                Order = 100,
                ConfigureClient = (client, _) =>
                {
                    var mq = new RabbitMQService(options)
                    {
                        UseRunSql = () => client.UseRunSQL()
                    };
                    client.useMessageQueue(mq);
                }
            });
        }

        public static SailClient UseRabbitMq(this SailClient client, Action<RabbitMqOptions> configure = null)
        {
            ArgumentNullException.ThrowIfNull(client);
            var options = new RabbitMqOptions();
            configure?.Invoke(options);
            var mq = new RabbitMQService(options)
            {
                UseRunSql = () => client.UseRunSQL()
            };
            client.useMessageQueue(mq);
            return client;
        }
    }
}
