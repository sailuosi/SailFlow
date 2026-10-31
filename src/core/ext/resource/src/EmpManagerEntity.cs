using mooSQL.data;

namespace saiflow.hub.Resource
{
    /// <summary>
    /// Employee Manager Entity
    /// 员工管理人员实体对象
    /// </summary>
    [SooTable(Name = "sys_employee_manager", Caption = "员工上级", Type = DBTableType.Table)]
    public class EmpManagerEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int? Id { get; set; }
        [SooColumn(Name = "employee_id")]
        public int? EmployeeId { get; set; }
        [SooColumn(Name = "employee_user_id")]
        public int? EmployeeUserId { get; set; }
        [SooColumn(Name = "manager_id")]
        public int? ManagerId { get; set; }
        [SooColumn(Name = "manager_user_id")]
        public int? ManagerUserId { get; set; }
    }
}
