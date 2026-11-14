namespace sailflow.engine.Ids
{
    /// <summary>AutoKey / Tag 约定名（与 <see cref="ISailIdSource"/> 配套）。</summary>
    public static class SailIdNames
    {
        /// <summary>SooColumn.AutoKey 正名；RegisterKeyGenerator 主 key。</summary>
        public const string AutoKey = "SailId";

        /// <summary>过渡期兼容旧实体 AutoKey="Guid"。</summary>
        public const string AutoKeyLegacyGuid = "Guid";

        /// <summary>SooColumn.Tag：列类型族（InitDb；与 AutoKey 正交）。</summary>
        public const string ColumnTag = "sail.id";
    }
}
