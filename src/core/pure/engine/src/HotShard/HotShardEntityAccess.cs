using System;
using System.Collections.Generic;
using mooSQL.data;
using sailflow.data;
using sailflow.engine.Client;
using sailflow.engine.Entity;

namespace sailflow.engine.HotShard
{
    /// <summary>按物理表名对实体做 CRUD（热分表路由后使用）。</summary>
    public static class HotShardEntityAccess
    {


        public static string ResolveProcessId(SQLBuilder kit, string processInstanceId)
        {
            if (kit == null || string.IsNullOrWhiteSpace(processInstanceId))
                return null;
            try
            {
                var pi = kit.findRowById<ProcessInstanceEntity>(processInstanceId);
                return pi?.ProcessId;
            }
            catch
            {
                return null;
            }
        }
    }
}
