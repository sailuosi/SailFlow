using System;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.engine.Entity;

namespace sailflow.data.Lifecycle
{
    /// <summary>将实体行写入指定物理表（保留主键值，供归档幂等）。</summary>
    public static class ArchiveRowCopier
    {
        public static void InsertPreservingKey<T>(SQLBuilder kit, string physicalTable, T entity) where T : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

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

        /// <summary>冷表是否已有该主键行（Exists 走 from 查询，不用裸 SQL）。</summary>
        public static bool ExistsById<T>(SQLBuilder kit, string physicalTable, string id) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;
            try
            {
                if (typeof(T) == typeof(ProcessInstanceEntity))
                    return ExistsProcessInstance(kit, physicalTable, id);
                if (typeof(T) == typeof(TaskEntity))
                    return ExistsTask(kit, physicalTable, id);
                if (typeof(T) == typeof(ActivityInstanceEntity))
                    return ExistsActivityInstance(kit, physicalTable, id);
                if (typeof(T) == typeof(TransitionInstanceEntity))
                    return ExistsTransitionInstance(kit, physicalTable, id);
                if (typeof(T) == typeof(ProcessVariableEntity))
                    return ExistsProcessVariable(kit, physicalTable, id);

                // 其它实体：列名来自映射主键，非手写字面量
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

        static bool ExistsProcessInstance(SQLBuilder kit, string physicalTable, string id)
        {
            var clip = kit.useClip();
            clip.from<ProcessInstanceEntity>(physicalTable, out var row)
                .where(() => row.ProcessInstanceId, id);
            return clip.select(row).queryList().Any();
        }

        static bool ExistsTask(SQLBuilder kit, string physicalTable, string id)
        {
            var clip = kit.useClip();
            clip.from<TaskEntity>(physicalTable, out var row)
                .where(() => row.TaskId, id);
            return clip.select(row).queryList().Any();
        }

        static bool ExistsActivityInstance(SQLBuilder kit, string physicalTable, string id)
        {
            var clip = kit.useClip();
            clip.from<ActivityInstanceEntity>(physicalTable, out var row)
                .where(() => row.ActivityInstanceId, id);
            return clip.select(row).queryList().Any();
        }

        static bool ExistsTransitionInstance(SQLBuilder kit, string physicalTable, string id)
        {
            var clip = kit.useClip();
            clip.from<TransitionInstanceEntity>(physicalTable, out var row)
                .where(() => row.TransitionInstanceId, id);
            return clip.select(row).queryList().Any();
        }

        static bool ExistsProcessVariable(SQLBuilder kit, string physicalTable, string id)
        {
            var clip = kit.useClip();
            clip.from<ProcessVariableEntity>(physicalTable, out var row)
                .where(() => row.ProcessVariableId, id);
            return clip.select(row).queryList().Any();
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
