namespace sailflow.engine.Hosting;

public static class SailBuilderKeys
{
    public static readonly object DbFactory = new();
    public static readonly object Cache = new();
    public static readonly object Config = new();
    public static readonly object ConfigAction = new();
    public static readonly object DataLifecycle = new();
    public static readonly object DataLifecycleAction = new();
    public static readonly object RuleEngines = new();
    public static readonly object ActionMethods = new();
    public static readonly object ScriptMethods = new();
    public static readonly object ServiceMethods = new();
    public static readonly object MessageQueue = new();

    /// <summary>办理人管道 <c>IAssigneePipeline</c>（由 sailflow.assignee UseAssignees 挂载）。</summary>
    public static readonly object AssigneePipeline = new();
    /// <summary>下一跳节点计算管道 <c>INextStepRoutePipeline</c>（UseNextStepRoute）。</summary>
    public static readonly object NextStepRoute = new();
    public static readonly object JsonSerializer = new();
    public static readonly object FormService = new();
    public static readonly object ResourceService = new();
    public static readonly object LocalizeMessages = new();
    public static readonly object FlowCapabilities = new();
    public static readonly object HookOutboxDrain = new();
    public static readonly object InstanceLockCleaner = new();

    /// <summary>A · 引擎四工厂（D2）。</summary>
    public static readonly object RuntimeFactory = new();
    public static readonly object MediatorFactory = new();
    public static readonly object ProcessModelFactory = new();
    public static readonly object ManagerFactory = new();

    /// <summary>发号 <see cref="SailIdBuilder"/>（UseSailIds）。</summary>
    public static readonly object SailIds = new();
}
