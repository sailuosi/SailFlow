using System;
using mooSQL.data;

namespace sailflow.biz.query
{
    /// <summary>流程抄送（产品表，非引擎表）。</summary>
    [SooTable(Name = "sf_process_instance_copy", Caption = "流程抄送", Type = DBTableType.Table)]
    public class SailProcessCopyEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64, Tag = "sail.id")]
        public string ProcessCopyId { get; set; }

        [SooColumn(Name = "user_id", Length = 64)]
        public string UserId { get; set; }

        [SooColumn(Name = "user_name", Length = 128)]
        public string UserName { get; set; }

        [SooColumn(Name = "start_user_id", Length = 64)]
        public string StartUserId { get; set; }

        [SooColumn(Name = "process_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }

        [SooColumn(Name = "process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        [SooColumn(Name = "process_name", Length = 255)]
        public string ProcessName { get; set; }

        [SooColumn(Name = "app_instance_id", Length = 128)]
        public string AppInstanceId { get; set; }

        [SooColumn(Name = "task_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string TaskId { get; set; }

        [SooColumn(Name = "task_name", Length = 255)]
        public string TaskName { get; set; }

        [SooColumn(Name = "creator_id", Length = 64)]
        public string CreatorId { get; set; }

        [SooColumn(Name = "creator_name", Length = 128)]
        public string CreatorName { get; set; }

        [SooColumn(Name = "reason", Length = 500)]
        public string Reason { get; set; }

        [SooColumn(Name = "create_time")]
        public DateTime? CreateTime { get; set; }
    }
}
