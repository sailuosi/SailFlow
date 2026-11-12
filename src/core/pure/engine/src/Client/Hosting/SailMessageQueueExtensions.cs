using System;
using sailflow.hub.Essential;
using sailflow.engine.Client;

namespace sailflow.engine.Hosting;

/// <summary>
/// 消息队列（IMessageQueueService）注册。
/// </summary>
public static class SailMessageQueueExtensions
{
    /// <summary>登记一个 <see cref="IMessageQueueService"/>；Build 时挂到 <see cref="SailClient"/>。</summary>
    public static ISailCollection UseMessageQueue(this ISailCollection services, IMessageQueueService messageQueue)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(messageQueue);
        services.Properties[SailBuilderKeys.MessageQueue] = messageQueue;
        services.ProvideFeature(SailFeatureNames.MessageQueue, order: 20);
        return services;
    }
}
