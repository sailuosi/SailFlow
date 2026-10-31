using mooSQL.data;

namespace saiflow.hub.Resource
{
    /// <summary>
    /// Department Entity
    /// 部门实体对象
    /// </summary>
    [SooTable(Name = "sys_department", Caption = "部门", Type = DBTableType.Table)]
    public class DeptEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int? Id { get; set; }
        [SooColumn(Name = "dept_code")]
        public string DeptCode { get; set; }
        [SooColumn(Name = "dept_name")]
        public string DeptName { get; set; }
        [SooColumn(Name = "parent_dept_id")]
        public int? ParentDeptId { get; set; }
        [SooColumn(Name = "description")]
        public string Description { get; set; }
    }
}
