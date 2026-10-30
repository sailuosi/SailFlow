using System;
using Microsoft.Extensions.Configuration;
using saiflow.engine.Config;

namespace saiflow.engine.Client;

/// <summary>
/// 引擎参数与特性配置（挂在 Client 上，替代散落静态配置主路径）。
/// </summary>
public class SailConfig
{
    private readonly SailClient _client;

    public SailConfig(SailClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        // 默认值对齐原 WfConfig
        ExpiredDays = WfConfig.EXPIRED_DAYS;
        ExpiredDaysEnabled = WfConfig.EXPIRED_DAYS_ENABLED;
        ExternalServiceFilePath = WfConfig.EXTERNAL_SERVICE_FILE_PATH;
    }

    public SailClient Client => _client;

    /// <summary>BPMN 缓存过期天数。</summary>
    public int ExpiredDays { get; set; }

    /// <summary>是否启用 BPMN 缓存过期。</summary>
    public bool ExpiredDaysEnabled { get; set; }

    /// <summary>本地外部服务 DLL 路径。</summary>
    public string ExternalServiceFilePath { get; set; }

    /// <summary>SMTP / Job 邮件相关（可由 IConfiguration 覆盖）。</summary>
    public string SmtpHost { get; set; } = JobAdminConfig.EMailSendUtility_SendEMailHost;

    public int SmtpPort { get; set; } = JobAdminConfig.EMailSendUtility_SendEMailHostPort;

    public string SmtpAccount { get; set; } = JobAdminConfig.EMailSendUtility_SendEMailAccount;

    public string SmtpPassword { get; set; } = JobAdminConfig.EMailSendUtility_SendEMailPassword;

    public bool SmtpSsl { get; set; }

    public bool SendEmailFlag { get; set; } = JobAdminConfig.EMailSendUtility_SendEMailFlag;

    public string LocalHostWebApp { get; set; } = JobAdminConfig.EMailSendUtility_LocalHostWebApp;

    /// <summary>从宿主 IConfiguration 填充（Email 段与 JobAdminConfig 约定一致）。</summary>
    public SailConfig ApplyConfiguration(IConfiguration configuration)
    {
        if (configuration == null)
            return this;

        JobAdminConfig.Configure(configuration);

        SmtpHost = configuration["Email:SmtpHost"] ?? SmtpHost;
        SmtpAccount = configuration["Email:Username"] ?? SmtpAccount;
        SmtpPassword = configuration["Email:Password"] ?? SmtpPassword;
        if (bool.TryParse(configuration["Email:EnableSsl"], out var ssl))
            SmtpSsl = ssl;
        if (int.TryParse(configuration["Email:SmtpPort"], out var port) && port > 0)
            SmtpPort = port;

        if (int.TryParse(configuration["Sailflow:ExpiredDays"], out var days) && days > 0)
            ExpiredDays = days;
        if (bool.TryParse(configuration["Sailflow:ExpiredDaysEnabled"], out var enabled))
            ExpiredDaysEnabled = enabled;
        ExternalServiceFilePath = configuration["Sailflow:ExternalServiceFilePath"] ?? ExternalServiceFilePath;

        return this;
    }
}
