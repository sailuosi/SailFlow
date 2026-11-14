using System;
using sailflow.engine.Ids;

namespace sailflow.runtime.Ids;

/// <summary>runtime 内置发号：Guid D 字符串；列兼容 uniqueidentifier；AutoKey 正名 SailId。</summary>
public sealed class GuidStringIdSource : ISailIdSource
{
    public static GuidStringIdSource Instance { get; } = new();

    public bool IsGuidCompatible => true;

    public string AutoKeyName => SailIdNames.AutoKey;

    public string NextEntityRowId() => Guid.NewGuid().ToString("D");

    public string NextProcessDesignId() => Guid.NewGuid().ToString("D");
}
