using mooSQL.data;

namespace saiflow.hub.Resource
{
    /// <summary>
    /// Role Entity
    /// 角色对象
    /// </summary>
    [SooTable(Name = "sys_role", Caption = "角色", Type = DBTableType.Table)]
    public class RoleEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [SooColumn(Name = "role_code")]
        public string RoleCode { get; set; }
        [SooColumn(Name = "role_name")]
        public string RoleName { get; set; }
    }
}
