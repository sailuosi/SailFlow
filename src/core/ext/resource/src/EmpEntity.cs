using mooSQL.data;

namespace saiflow.hub.Resource
{
    /// <summary>
    /// Employee Entity
    /// 员工实体对象
    /// </summary>
    [SooTable(Name = "sys_employee", Caption = "员工", Type = DBTableType.Table)]
    public class EmpEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [SooColumn(Name = "dept_id")]
        public int DeptId { get; set; }
        [SooColumn(Name = "emp_code")]
        public string EmpCode { get; set; }
        [SooColumn(Name = "emp_name")]
        public string EmpName { get; set; }
        [SooColumn(Name = "user_id")]
        public int UserId { get; set; }
        [SooColumn(Name = "mobile")]
        public string Mobile { get; set; }
        [SooColumn(Name = "email")]
        public string EMail { get; set; }
        [SooColumn(Name = "manager_id")]
        public int ManagerId { get; set; }
        [SooColumn(Name = "remark")]
        public string Remark { get; set; }
    }
}
