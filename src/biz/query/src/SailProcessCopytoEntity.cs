using System;
using mooSQL.data;

namespace sailflow.biz.query
{
    /// <summary>流程抄送（产品表，非引擎表）。抄送语义用 copyto，禁止裸 copy（易误解为复制）。</summary>
    [SooTable(Name = "sf_proc_copyto", Caption = "流程抄送", Type = DBTableType.Table)]
    public class SailProcessCopytoEntity
    {
        [SooColumn(Name = "ct_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string ProcessCopytoId { get; set; }

        [SooColumn(Name = "ct_user_id", Length = 64)]
        public string UserId { get; set; }

        [SooColumn(Name = "ct_user_name", Length = 128)]
        public string UserName { get; set; }

        [SooColumn(Name = "ct_start_user_id", Length = 64)]
        public string StartUserId { get; set; }

        [SooColumn(Name = "ct_proc_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }

        [SooColumn(Name = "ct_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        [SooColumn(Name = "ct_proc_name", Length = 255)]
        public string ProcessName { get; set; }

        [SooColumn(Name = "ct_biz_pk", Length = 128)]
        public string BizPK { get; set; }

        [SooColumn(Name = "ct_task_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string TaskId { get; set; }

        [SooColumn(Name = "ct_task_name", Length = 255)]
        public string TaskName { get; set; }

        [SooColumn(Name = "ct_crt_user_id", Length = 64)]
        public string CreatorId { get; set; }

        [SooColumn(Name = "ct_crt_user_name", Length = 128)]
        public string CreatorName { get; set; }

        [SooColumn(Name = "ct_reason", Length = 500)]
        public string Reason { get; set; }

        [SooColumn(Name = "ct_crt_time")]
        public DateTime? CreateTime { get; set; }
    }
}
