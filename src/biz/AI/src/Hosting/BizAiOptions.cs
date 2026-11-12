namespace sailflow.AI.Hosting;

public sealed class BizAiOptions
{
    /// <summary>Register default IAiNodeExecutor implementation.</summary>
    public bool RegisterExecutor { get; set; } = true;

    /// <summary>Ensure AI tables (ai_model_provider / ai_activity_config) after Build.</summary>
    public bool EnsureTables { get; set; } = true;
}
