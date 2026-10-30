using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;

namespace saiflow.engine.Business.SqlProvider
{
    /// <summary>
    /// Phase E：手写 SQL 的方言名/分页片段（读 mooSQL <see cref="DataBaseType"/>，经 <see cref="SailDbAccess"/>）。
    /// </summary>
    public static class SailSqlDialect
    {
        public static bool IsSqlServer
            => SailDbAccess.IsConfigured
               && SailDbAccess.GetRequired().config?.dbType == DataBaseType.MSSQL;

        /// <summary>流程定义表（实体 / Manager 内联 SQL，固定 snake）。</summary>
        public static string ProcessEntityTable => "wf_process";

        /// <summary>变量定义表。</summary>
        public static string VariableTable => "wf_variable";

        /// <summary>活动实例表。</summary>
        public static string ActivityInstanceTable => "wf_activity_instance";

        /// <summary>流程实例表。</summary>
        public static string ProcessInstanceTable => "wf_process_instance";

        /// <summary>任务表。</summary>
        public static string TaskTable => "wf_task";

        /// <summary>转移实例表。</summary>
        public static string TransitionInstanceTable => "wf_transition_instance";

        /// <summary>流程变量表。</summary>
        public static string ProcessVariableTable => "wf_process_variable";

        /// <summary>任务明细视图。</summary>
        public static string TaskView
            => IsSqlServer ? "vwWfActivityInstanceTasks" : "vw_wf_task_details";

        /// <summary>任务视图主键排序列（SQL Server 用 TaskId，其它库视图主键为 id）。</summary>
        public static string TaskIdColumn
            => IsSqlServer ? "TaskId" : "id";

        /// <summary>实体字段 → 库列名（SQL Server 保持 Pascal）。</summary>
        public static string MapField(string entityField)
        {
            if (IsSqlServer)
                return entityField;

            if (FieldMappings.TryGetValue(entityField, out var dbField))
                return dbField;

            return ToSnake(entityField);
        }

        private static readonly Dictionary<string, string> FieldMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            ["TaskId"] = "id",
            ["ProcessId"] = "process_id",
            ["AppInstanceId"] = "app_instance_id",
            ["AssignedUserId"] = "assigned_user_id",
            ["EndedUserId"] = "ended_user_id",
            ["AppName"] = "app_name",
            ["ProcessState"] = "process_state",
            ["ActivityType"] = "activity_type",
            ["WorkItemType"] = "work_item_type",
            ["ActivityState"] = "activity_state",
            ["TaskState"] = "task_state",
            ["MainActivityState"] = "main_activity_state",
            ["Version"] = "version",
            ["ActivityInstanceId"] = "activity_instance_id",
            ["ProcessInstanceId"] = "process_instance_id",
            ["IsEMailSent"] = "is_email_sent",
        };

        private static string ToSnake(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            return string.Concat(input.Select((c, i) =>
                i > 0 && char.IsUpper(c) ? "_" + c : c.ToString()));
        }
    }
}
