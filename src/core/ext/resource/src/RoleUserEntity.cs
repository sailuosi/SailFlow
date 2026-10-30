using mooSQL.data;

namespace saiflow.hub.Resource
{
    /// <summary>
    /// Role User Entity
    /// 角色用户关联表
    /// </summary>
    [SooTable(Name = "sys_role_user", Caption = "角色用户", Type = DBTableType.Table)]
    public class RoleUserEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [SooColumn(Name = "role_id")]
        public int RoleId { get; set; }
        [SooColumn(Name = "user_id")]
        public int UserId { get; set; }
    }
}
