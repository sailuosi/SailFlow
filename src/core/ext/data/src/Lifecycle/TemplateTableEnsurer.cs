using System;
using System.Collections.Concurrent;
using mooSQL.data;
using mooSQL.data.model;

namespace sailflow.data.Lifecycle
{
    /// <summary>
    /// 分表 / 冷表 ENSURE：复用 mooSQL 已有能力。
    /// <list type="number">
    /// <item>模板表已存在 → <see cref="DDLBuilder.doCopyTableScheme"/>（方言：SQL Server SELECT INTO…WHERE 1=2 等）</item>
    /// <item>否则有实体类型 → <see cref="DBTableCreator.createTable{T}(string, string)"/> / <see cref="DBTableCreator.CreateTable"/></item>
    /// </list>
    /// </summary>
    public static class TemplateTableEnsurer
    {
        static readonly ConcurrentDictionary<string, byte> Ensured =
            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 按现有模板表或实体 ENSURE 物理表。
        /// </summary>
        /// <param name="kit">库连接。</param>
        /// <param name="physicalTable">目标物理表名。</param>
        /// <param name="templateTable">可选模板表；存在则优先拷结构。</param>
        /// <param name="entityType">可选实体；模板表不存在时按实体建表。</param>
        /// <param name="configure">可选：如 <c>columnTypeByTag("sail.id", Guid)</c>，与宿主 InitDb 对齐。</param>
        /// <param name="rethrow">失败是否抛出。</param>
        public static void Ensure(
            SQLBuilder kit,
            string physicalTable,
            string templateTable = null,
            Type entityType = null,
            Action<DBTableCreator> configure = null,
            bool rethrow = true)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));
            if (string.IsNullOrWhiteSpace(physicalTable))
                return;
            if (!string.IsNullOrWhiteSpace(templateTable)
                && string.Equals(physicalTable, templateTable, StringComparison.OrdinalIgnoreCase)
                && entityType == null)
                return;
            if (!Ensured.TryAdd(physicalTable, 0))
                return;

            try
            {
                var ddl = kit.useDDL();
                if (ddl.hasTable(physicalTable))
                    return;

                if (!string.IsNullOrWhiteSpace(templateTable)
                    && ddl.hasTable(templateTable)
                    && TryCopyFromTemplate(kit, ddl, templateTable.Trim(), physicalTable.Trim()))
                    return;

                if (entityType != null)
                {
                    CreateFromEntity(kit, entityType, physicalTable.Trim(), configure);
                    return;
                }

                throw new InvalidOperationException(
                    "Cannot ENSURE table '" + physicalTable
                    + "': template '" + (templateTable ?? "")
                    + "' missing and no entityType provided.");
            }
            catch
            {
                Ensured.TryRemove(physicalTable, out _);
                if (rethrow)
                    throw;
            }
        }

        /// <summary>仅按实体建表（无模板）。</summary>
        public static void EnsureFromEntity<T>(
            SQLBuilder kit,
            string physicalTable,
            Action<DBTableCreator> configure = null,
            bool rethrow = true)
            where T : class
            => Ensure(kit, physicalTable, templateTable: null, entityType: typeof(T), configure, rethrow);

        /// <summary>仅从现有表拷结构。</summary>
        public static void EnsureFromTable(
            SQLBuilder kit,
            string physicalTable,
            string templateTable,
            bool rethrow = true)
            => Ensure(kit, physicalTable, templateTable, entityType: null, configure: null, rethrow);

        /// <summary>
        /// 宿主常用 Tag：与 Schema InitDb 对齐（Guid 主键 / Json 大字段）。
        /// 不强制调用方使用；拷模板表时已继承现网类型，通常无需再配。
        /// </summary>
        public static void ApplySailColumnTags(DBTableCreator creator)
        {
            if (creator == null)
                return;
            creator
                .columnTypeByTag("sail.id", DataFam.Guid)
                .columnTypeByTag("sail.xml", DataFam.Json);
        }

        /// <summary>测试或运维：清除 ENSURE 缓存。</summary>
        public static void ClearCache() => Ensured.Clear();

        static bool TryCopyFromTemplate(
            SQLBuilder kit,
            DDLBuilder ddl,
            string templateTable,
            string physicalTable)
        {
            var cmd = ddl.toCopyTableScheme(templateTable, physicalTable);
            var sql = cmd?.sql?.Trim() ?? "";
            if (string.IsNullOrEmpty(sql))
                return false;

            ddl.doCopyTableScheme(templateTable, physicalTable);
            return true;
        }

        static void CreateFromEntity(
            SQLBuilder kit,
            Type entityType,
            string physicalTable,
            Action<DBTableCreator> configure)
        {
            var creator = kit.useDBInit().autoMode();
            configure?.Invoke(creator);

            var en = kit.DBLive.client.EntityCash.getEntityInfo(entityType);
            if (en == null)
                throw new InvalidOperationException(
                    "Entity metadata not found for " + entityType.FullName);

            creator.CreateTable(en, physicalTable, creator.CreateMode ?? "createAuto");
        }
    }
}
