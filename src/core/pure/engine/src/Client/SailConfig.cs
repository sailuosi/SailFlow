using System;
using saiflow.engine.Config;

namespace saiflow.engine.Client;

/// <summary>
/// 引擎运行参数形状（类比 mooSQL <c>DBPosition</c>）。
/// 仅定义字段与默认值；不负责从 IConfiguration / AppSettings 等加载——由业务侧注册填入。
/// 邮件 SMTP 不在此形状；见 <c>saiflow.msg.TaskMailOptions</c> + <c>UseDefaultTaskMail</c>。
/// </summary>
public class SailConfig
{
    private readonly SailClient _client;

    public SailConfig(SailClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ExpiredDays = WfConfig.EXPIRED_DAYS;
        ExpiredDaysEnabled = WfConfig.EXPIRED_DAYS_ENABLED;
        ExternalServiceFilePath = WfConfig.EXTERNAL_SERVICE_FILE_PATH;
    }

    public SailClient Client => _client;

    /// <summary>BPMN 缓存过期天数。</summary>
    public int ExpiredDays { get; set; }

    /// <summary>是否启用 BPMN 流程模型缓存。</summary>
    public bool ExpiredDaysEnabled { get; set; }

    /// <summary>本地外部服务 DLL 路径。</summary>
    public string ExternalServiceFilePath { get; set; }

    /// <summary>
    /// 流程定义文件存储类型；为 <c>localfile</c> 时使用本地文件存储。
    /// </summary>
    public string WorkflowFileStorageType { get; set; }

    /// <summary>本地流程文件根目录（与 <see cref="WorkflowFileStorageType"/>=<c>localfile</c> 配合）。</summary>
    public string WorkflowFileServer { get; set; }

    /// <summary>用委托覆盖本实例字段（注册用）。</summary>
    public SailConfig Configure(Action<SailConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(this);
        return this;
    }

    /// <summary>从另一形状实例拷贝非空/有意义的值（业务侧装载后合并）。</summary>
    public SailConfig Apply(SailConfig source)
    {
        if (source == null)
            return this;

        ExpiredDays = source.ExpiredDays;
        ExpiredDaysEnabled = source.ExpiredDaysEnabled;
        if (source.ExternalServiceFilePath != null)
            ExternalServiceFilePath = source.ExternalServiceFilePath;
        if (source.WorkflowFileStorageType != null)
            WorkflowFileStorageType = source.WorkflowFileStorageType;
        if (source.WorkflowFileServer != null)
            WorkflowFileServer = source.WorkflowFileServer;
        return this;
    }
}
