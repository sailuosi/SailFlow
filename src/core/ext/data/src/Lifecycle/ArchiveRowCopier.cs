using System;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.data.Storage;

namespace sailflow.data.Lifecycle
{
    /// <summary>
    /// 归档行拷贝兼容入口；实现已迁 <see cref="DefaultProcessArchiveStorage"/>。
    /// 新代码优先经 <c>IProcessArchiveStorage</c>。
    /// </summary>
    public static class ArchiveRowCopier
    {
        static readonly DefaultProcessArchiveStorage Impl = new();

        public static void InsertPreservingKey<T>(SQLBuilder kit, string physicalTable, T entity) where T : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity is ProcessInstanceEntity pi)
            {
                Impl.InsertPreservingKeyProcessInstance(kit, physicalTable, pi);
                return;
            }
            if (entity is TaskEntity task)
            {
                Impl.InsertPreservingKeyTask(kit, physicalTable, task);
                return;
            }
            if (entity is ActivityInstanceEntity ai)
            {
                Impl.InsertPreservingKeyActivityInstance(kit, physicalTable, ai);
                return;
            }
            if (entity is TrackEntity tr)
            {
                Impl.InsertPreservingKeyTransition(kit, physicalTable, tr);
                return;
            }
            if (entity is ProcessVariableEntity pv)
            {
                Impl.InsertPreservingKeyProcessVariable(kit, physicalTable, pv);
                return;
            }

            // 其它实体：保留原泛型列映射路径
            var maps = SailEntityMapper.GetColumnMaps<T>();
            kit.setTable(physicalTable).newRow();
            foreach (var map in maps)
            {
                var val = map.GetValue(entity);
                if (val == null)
                    continue;
                if (map.IsKey && IsDefaultKey(val))
                    continue;
                kit.set(map.ColumnName, val);
            }
            kit.doInsert();
        }

        /// <summary>冷表是否已有该主键行。</summary>
        public static bool ExistsById<T>(SQLBuilder kit, string physicalTable, string id) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;
            if (typeof(T) == typeof(ProcessInstanceEntity))
                return Impl.ExistsProcessInstance(kit, physicalTable, id);
            if (typeof(T) == typeof(TaskEntity))
                return Impl.ExistsTask(kit, physicalTable, id);
            if (typeof(T) == typeof(ActivityInstanceEntity))
                return Impl.ExistsActivityInstance(kit, physicalTable, id);
            if (typeof(T) == typeof(TrackEntity))
                return Impl.ExistsTransition(kit, physicalTable, id);
            if (typeof(T) == typeof(ProcessVariableEntity))
                return Impl.ExistsProcessVariable(kit, physicalTable, id);

            try
            {
                var keyCol = SailEntityMapper.GetColumnMaps<T>().FirstOrDefault(m => m.IsKey)?.ColumnName;
                if (string.IsNullOrWhiteSpace(keyCol))
                    return false;
                var clip = kit.useClip();
                clip.from<T>(physicalTable, out var row)
                    .where(keyCol, id);
                return clip.select(row).queryList().Any();
            }
            catch
            {
                return false;
            }
        }

        private static bool IsDefaultKey(object val)
        {
            if (val == null)
                return true;
            if (val is int i)
                return i == 0;
            if (val is long l)
                return l == 0L;
            if (val is short s)
                return s == 0;
            if (val is string str)
                return string.IsNullOrWhiteSpace(str);
            return false;
        }
    }
}
