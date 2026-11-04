namespace saiflow.assignee;

/// <summary>R10：当前 Performer 列表来源（供 Filter 自决是否改写；F1 可追踪）。</summary>
public static class PerformerSources
{
    public const string Specific = "Specific";
    public const string Resolver = "Resolver";
    public const string Mixed = "Mixed";
    public const string EmptyAction = "EmptyAction";
    public const string Unknown = "Unknown";
}
