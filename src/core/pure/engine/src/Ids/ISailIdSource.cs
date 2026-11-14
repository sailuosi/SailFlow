namespace sailflow.engine.Ids
{
    /// <summary>
    /// 引擎统一 string Id 发号源。实现放 runtime/宿主；pure 仅契约 + 默认 Guid 实现。
    /// 每种 Id 一个方法，不用枚举分发。
    /// </summary>
    public interface ISailIdSource
    {
        /// <summary>实体行主键（对应 SooColumn AutoKey 插入路径）。</summary>
        string NextEntityRowId();

        /// <summary>流程设计定义键 ProcessId（与 Version 配对；不是行 PK）。</summary>
        string NextProcessDesignId();

        /// <summary>是否可写入 sail.id → uniqueidentifier 列。</summary>
        bool IsGuidCompatible { get; }

        /// <summary>mooSQL AutoKey 注册正名；默认 <see cref="SailIdNames.AutoKey"/>。</summary>
        string AutoKeyName { get; }
    }
}
