namespace sailflow.AI.Hosting;

public sealed class BizAiOptions
{
    /// <summary>Register default IAiNodeExecutor implementation.</summary>
    public bool RegisterExecutor { get; set; } = true;

    /// <summary>
    /// 默认 false：建表归属业务宿主。若仍为 true，PostBuild 为空操作（AiRuntime.EnsureTables 已废弃）。
    /// </summary>
    public bool EnsureTables { get; set; } = false;
}
