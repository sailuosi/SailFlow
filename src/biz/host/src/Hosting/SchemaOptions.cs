namespace sailflow.biz.Hosting;

/// <summary>已废弃：建表归属宿主。保留类型以免旧编译引用断裂。</summary>
[Obsolete("UseSchema/SchemaOptions 不再驱动建表；请在业务宿主实现 SchemaInitializer。")]
public sealed class SchemaOptions
{
    public bool IncludeAi { get; set; } = true;
    public bool Seed { get; set; }
}
