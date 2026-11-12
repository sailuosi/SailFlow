namespace sailflow.biz.Hosting;

public sealed class SchemaOptions
{
    /// <summary>Also ensure AI tables via SailSchemaInitializer.</summary>
    public bool IncludeAi { get; set; } = true;

    /// <summary>Insert minimal process seed when process table is empty.</summary>
    public bool Seed { get; set; }
}
