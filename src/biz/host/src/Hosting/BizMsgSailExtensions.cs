using System;
using Microsoft.Extensions.Configuration;
using sailflow.engine.Hosting;
using sailflow.msg;
using sailflow.msg.Hosting;

namespace sailflow.biz.Hosting;

/// <summary>
/// Biz composition：启用消息队列（RabbitMQ）与默认待办邮件。
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

    /// <summary>可选：启用默认待办 SMTP 邮件（显式调用才发信；选项在 Use 时刻成型）。</summary>
    public static ISailCollection UseBizTaskMail(
        this ISailCollection services,
        Action<TaskMailOptions> configure = null) =>
        services.UseDefaultTaskMail(configure);

    public static SailClientBuilder UseBizTaskMail(
        this SailClientBuilder builder,
        Action<TaskMailOptions> configure = null) =>
        builder.UseDefaultTaskMail(configure);

    /// <summary>从宿主 <see cref="IConfiguration"/> 的 Email 段装载后启用默认待办邮件。</summary>
    public static ISailCollection UseBizTaskMail(
        this ISailCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        return services.UseDefaultTaskMail(o => ApplyEmailFromConfiguration(o, configuration));
    }

    public static SailClientBuilder UseBizTaskMail(
        this SailClientBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        return builder.UseDefaultTaskMail(o => ApplyEmailFromConfiguration(o, configuration));
    }

    /// <summary>设计别名：与 <see cref="UseBizTaskMail"/> 相同，启用默认待办 SMTP。</summary>
    public static ISailCollection UseBizTaskNotify(
        this ISailCollection services,
        Action<TaskMailOptions> configure = null) =>
        services.UseDefaultTaskMail(configure);

    public static SailClientBuilder UseBizTaskNotify(
        this SailClientBuilder builder,
        Action<TaskMailOptions> configure = null) =>
        builder.UseDefaultTaskMail(configure);

    public static ISailCollection UseBizTaskNotify(
        this ISailCollection services,
        IConfiguration configuration) =>
        services.UseBizTaskMail(configuration);

    public static SailClientBuilder UseBizTaskNotify(
        this SailClientBuilder builder,
        IConfiguration configuration) =>
        builder.UseBizTaskMail(configuration);

    /// <summary>Email 段 → <see cref="TaskMailOptions"/>（键与历史 ApplyFromConfiguration 一致）。</summary>
    public static void ApplyEmailFromConfiguration(TaskMailOptions options, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (configuration == null)
            return;

        options.SmtpHost = configuration["Email:SmtpHost"] ?? options.SmtpHost;
        options.SmtpAccount = configuration["Email:Username"] ?? options.SmtpAccount;
        options.SmtpPassword = configuration["Email:Password"] ?? options.SmtpPassword;
        if (bool.TryParse(configuration["Email:EnableSsl"], out var ssl))
            options.SmtpSsl = ssl;
        if (int.TryParse(configuration["Email:SmtpPort"], out var port) && port > 0)
            options.SmtpPort = port;

        options.AppBaseUrl = configuration["Sailflow:LocalHostWebApp"]
                             ?? configuration["Email:LocalHostWebApp"]
                             ?? options.AppBaseUrl;
    }
}
