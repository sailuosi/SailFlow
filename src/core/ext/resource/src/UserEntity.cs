using mooSQL.data;

namespace saiflow.hub.Resource
{
    [SooTable(Name = "sys_user", Caption = "用户", Type = DBTableType.Table)]
    public class UserEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        [SooColumn(Name = "user_name")]
        public string UserName { get; set; }
        [SooColumn(Name = "email")]
        public string EMail { get; set; }
    }
}
