using System;
using Microsoft.Extensions.Configuration;
using saiflow.engine.Client;
using saiflow.engine.Client.Hosting;
using AppSettingsManager = System.Configuration.ConfigurationManager;

namespace saiflow.biz.Hosting;

/// <summary>
/// 业务侧配置装载：把 IConfiguration / AppSettings 填入引擎 <see cref="SailConfig"/> 形状后注册。
/// 引擎本身不依赖具体配置源。
/// </summary>
public static class SailConfigBizExtensions
{
    /// <summary>
    /// 从宿主 <see cref="IConfiguration"/> 装载 Email / Sailflow 段并 <c>UseConfig</c>。
    /// </summary>
    public static SailClientBuilder UseConfigFrom(
        this SailClientBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        return builder.UseConfig(cfg => ApplyFromConfiguration(cfg, configuration));
    }

    /// <summary>服务集合上登记：Build 时用 IConfiguration 填充形状。</summary>
    public static ISailCollection UseConfigFrom(
        this ISailCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        return services.UseConfig(cfg => ApplyFromConfiguration(cfg, configuration));
    }

    /// <summary>
    /// 兼容旧 AppSettings：WorkflowFileStorageType / WorkflowFileServer 等。
    /// </summary>
    public static SailClientBuilder UseConfigFromAppSettings(this SailClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseConfig(ApplyFromAppSettings);
    }

    public static ISailCollection UseConfigFromAppSettings(this ISailCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.UseConfig(ApplyFromAppSettings);
    }

    /// <summary>带 IConfiguration 的 CreateBuilder 便捷入口。</summary>
    public static SailClientBuilder CreateBuilder(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return SailClient.CreateBuilder().UseConfigFrom(configuration);
    }

    /// <summary>把 IConfiguration 键写入已有形状。</summary>
    public static void ApplyFromConfiguration(SailConfig config, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (configuration == null)
            return;

        config.SmtpHost = configuration["Email:SmtpHost"] ?? config.SmtpHost;
        config.SmtpAccount = configuration["Email:Username"] ?? config.SmtpAccount;
        config.SmtpPassword = configuration["Email:Password"] ?? config.SmtpPassword;
        if (bool.TryParse(configuration["Email:EnableSsl"], out var ssl))
            config.SmtpSsl = ssl;
        if (int.TryParse(configuration["Email:SmtpPort"], out var port) && port > 0)
            config.SmtpPort = port;

        config.LocalHostWebApp = configuration["Sailflow:LocalHostWebApp"]
                                 ?? configuration["Email:LocalHostWebApp"]
                                 ?? config.LocalHostWebApp;

        if (int.TryParse(configuration["Sailflow:ExpiredDays"], out var days) && days > 0)
            config.ExpiredDays = days;
        if (bool.TryParse(configuration["Sailflow:ExpiredDaysEnabled"], out var enabled))
            config.ExpiredDaysEnabled = enabled;

        config.ExternalServiceFilePath = configuration["Sailflow:ExternalServiceFilePath"]
                                         ?? config.ExternalServiceFilePath;
        config.WorkflowFileStorageType = configuration["Sailflow:WorkflowFileStorageType"]
                                         ?? configuration["WorkflowFileStorageType"]
                                         ?? config.WorkflowFileStorageType;
        config.WorkflowFileServer = configuration["Sailflow:WorkflowFileServer"]
                                    ?? configuration["WorkflowFileServer"]
                                    ?? config.WorkflowFileServer;
    }

    /// <summary>把 ConfigurationManager.AppSettings 写入形状（遗留 .config）。</summary>
    public static void ApplyFromAppSettings(SailConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        string Get(string key)
        {
            try { return AppSettingsManager.AppSettings[key]; }
            catch { return null; }
        }

        var storageType = Get("WorkflowFileStorageType");
        if (!string.IsNullOrEmpty(storageType))
            config.WorkflowFileStorageType = storageType;

        var server = Get("WorkflowFileServer");
        if (!string.IsNullOrEmpty(server))
            config.WorkflowFileServer = server;

        var expiredDays = Get("Sailflow:ExpiredDays") ?? Get("ExpiredDays");
        if (int.TryParse(expiredDays, out var days) && days > 0)
            config.ExpiredDays = days;

        var expiredEnabled = Get("Sailflow:ExpiredDaysEnabled") ?? Get("ExpiredDaysEnabled");
        if (bool.TryParse(expiredEnabled, out var enabled))
            config.ExpiredDaysEnabled = enabled;

        var webApp = Get("LocalHostWebApp");
        if (!string.IsNullOrEmpty(webApp))
            config.LocalHostWebApp = webApp;

        var smtpHost = Get("SmtpHost") ?? Get("Email:SmtpHost");
        if (!string.IsNullOrEmpty(smtpHost))
            config.SmtpHost = smtpHost;
    }
}
