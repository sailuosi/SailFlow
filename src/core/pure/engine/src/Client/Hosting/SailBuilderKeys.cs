namespace saiflow.engine.Client.Hosting;

public static class SailBuilderKeys
{
    public static readonly object DbFactory = new();
    public static readonly object Cache = new();
    public static readonly object Config = new();
    public static readonly object ConfigAction = new();
    public static readonly object RuleEngines = new();
    public static readonly object ActionMethods = new();
    public static readonly object ScriptMethods = new();
    public static readonly object ServiceMethods = new();
    public static readonly object MessageQueue = new();
    public static readonly object FormService = new();
    public static readonly object ResourceService = new();
    public static readonly object LocalizeMessages = new();
    public static readonly object WorkflowCapabilities = new();
    public static readonly object HookOutboxDrain = new();
}
