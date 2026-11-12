using System;
using mooSQL.data;

namespace sailflow.data.Schema
{
    /// <summary>
    /// P0：待办/实例热路径索引（IF NOT EXISTS；方言以 SQLite 为主，其它库失败则忽略）。
    /// </summary>
    public static class HotPathIndexEnsurer
    {
        public static void Ensure(SQLBuilder kit)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));

            TryCreateIndex(kit, "IX_wf_task_pi",
                "CREATE INDEX IF NOT EXISTS IX_wf_task_pi ON wf_task(process_instance_id)");
            TryCreateIndex(kit, "IX_wf_task_assignee_state",
                "CREATE INDEX IF NOT EXISTS IX_wf_task_assignee_state ON wf_task(assigned_user_id, task_state, process_instance_id)");
            TryCreateIndex(kit, "IX_wf_task_state_created",
                "CREATE INDEX IF NOT EXISTS IX_wf_task_state_created ON wf_task(task_state, created_datetime)");
            TryCreateIndex(kit, "IX_wf_task_state_overdue",
                "CREATE INDEX IF NOT EXISTS IX_wf_task_state_overdue ON wf_task(task_state, overdue_datetime)");
            TryCreateIndex(kit, "IX_wf_ai_pi_state",
                "CREATE INDEX IF NOT EXISTS IX_wf_ai_pi_state ON wf_activity_instance(process_instance_id, activity_state)");
            TryCreateIndex(kit, "IX_wf_pi_state_created",
                "CREATE INDEX IF NOT EXISTS IX_wf_pi_state_created ON wf_process_instance(process_state, created_datetime)");
            TryCreateIndex(kit, "IX_wf_pi_creator_state",
                "CREATE INDEX IF NOT EXISTS IX_wf_pi_creator_state ON wf_process_instance(created_user_id, process_state, created_datetime)");
            TryCreateIndex(kit, "IX_wf_pv_pi_name",
                "CREATE INDEX IF NOT EXISTS IX_wf_pv_pi_name ON wf_process_variable(process_instance_id, name)");
            TryCreateIndex(kit, "IX_wf_ti_pi",
                "CREATE INDEX IF NOT EXISTS IX_wf_ti_pi ON wf_transition_instance(process_instance_id)");
            TryCreateIndex(kit, "IX_wf_hook_outbox_status",
                "CREATE INDEX IF NOT EXISTS IX_wf_hook_outbox_status ON wf_hook_outbox(status, next_retry_at)");
        }

        static void TryCreateIndex(SQLBuilder kit, string name, string ddl)
        {
            try
            {
                kit.exeNonQuery(ddl);
            }
            catch
            {
                // 非 SQLite 或缺表时忽略
            }
        }
    }
}
