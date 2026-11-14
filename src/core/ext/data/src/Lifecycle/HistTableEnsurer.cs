using System;
using mooSQL.data;

namespace sailflow.data.Lifecycle
{
    /// <summary>
    /// 按年冷表 ENSURE：优先拷热表结构；热表不存在则按闭包实体建表。
    /// </summary>
    public static class HistTableEnsurer
    {
        /// <summary>按 <see cref="HistTableSpec"/>（实体 + 热表名）ENSURE 冷表。</summary>
        public static void Ensure(SQLBuilder kit, HistTableSpec spec, string histTable)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            TemplateTableEnsurer.Ensure(
                kit,
                histTable,
                templateTable: spec.HotTableName,
                entityType: spec.EntityType,
                configure: TemplateTableEnsurer.ApplySailColumnTags,
                rethrow: true);
        }

        /// <summary>兼容旧调用：仅按热表名拷结构（无实体回退）。</summary>
        public static void Ensure(SQLBuilder kit, string histTable, string hotTable) =>
            TemplateTableEnsurer.EnsureFromTable(kit, histTable, hotTable, rethrow: true);
    }
}
