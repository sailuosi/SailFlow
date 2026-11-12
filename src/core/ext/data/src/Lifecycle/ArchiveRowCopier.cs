using System;
using mooSQL.data;
using sailflow.data;

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

        public static bool ExistsById(SQLBuilder kit, string physicalTable, int id)
        {
            try
            {
                var dt = kit.exeQuery("SELECT 1 AS x FROM " + physicalTable + " WHERE id = " + id);
                return dt != null && dt.Rows.Count > 0;
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
            return false;
        }
    }
}
