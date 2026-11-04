using System;
using saiflow.engine.Hosting;
using saiflow.msg;
using saiflow.msg.Hosting;

namespace saiflow.biz.Hosting;

/// <summary>
/// Biz composition：启用消息队列（RabbitMQ）。
/// </summary>
public static class BizMsgSailExtensions
{
    /// <summary>
    /// 注册 RabbitMQ <c>IMessageQueueService</c>；之后仍可用 <c>UseMessageQueue</c> 覆盖自定义实现。
    /// </summary>
    public static ISailCollection UseBizMsg(this ISailCollection services, Action<RabbitMqOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.UseRabbitMq(configure);
    }

    public static SailClientBuilder UseBizMsg(this SailClientBuilder builder, Action<RabbitMqOptions> configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseRabbitMq(configure);
    }

    /// <summary>可选：启用默认待办 SMTP 邮件（显式调用才发信）。</summary>
    public static ISailCollection UseBizTaskMail(this ISailCollection services) =>
        services.UseDefaultTaskMail();

    public static SailClientBuilder UseBizTaskMail(this SailClientBuilder builder) =>
        builder.UseDefaultTaskMail();

    /// <summary>设计别名：与 <see cref="UseBizTaskMail"/> 相同，启用默认待办 SMTP。</summary>
    public static ISailCollection UseBizTaskNotify(this ISailCollection services) =>
        services.UseDefaultTaskMail();

    public static SailClientBuilder UseBizTaskNotify(this SailClientBuilder builder) =>
        builder.UseDefaultTaskMail();
}
