namespace saiflow.engine.Hosting;

/// <summary>
/// 已约定的能力名（<see cref="SailFeatureDescriptor.Name"/> / <see cref="SailFeatureDescriptor.DependsOn"/>）。
/// 新增 UseXxx 时优先复用常量，避免拼写漂移。
/// </summary>
public static class SailFeatureNames
{
    public const string Database = "database";
    public const string Json = "json";
    public const string MessageQueue = "message-queue";
    public const string Assignee = "assignee";
    public const string FlowGate = "flow-gate";
    public const string FlowHook = "flow-hook";
    public const string TaskNotifier = "task-notifier";
    public const string HookOutboxDrain = "HookOutboxDrain";
    public const string HookOutboxDrainAutoStart = "HookOutboxDrain.AutoStart";
    public const string Form = "Form";
    public const string Resource = "Resource";
    public const string Localize = "Localize";
    public const string Schema = "Schema";
    public const string SailData = "sail.data";
    public const string ImmediateArchive = "sail.lifecycle.archive.immediate";
    public const string RabbitMq = "saiflow.msg.RabbitMq";
    public const string DefaultTaskMail = "saiflow.msg.DefaultMail";
    public const string BizAssigneeSamples = "biz-assignee-samples";
}
