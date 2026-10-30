using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;

namespace saiflow.hub.Resource
{
    /// <summary>用户。终态：SailSqlAccess / kit。</summary>
    internal class UserManager
    {
        internal List<UserEntity> GetAll()
        {
            var kit = SailSqlAccess.UseRunSQL();
            return kit
                .select("*")
                .from("sys_user")
                .orderBy("user_name")
                .query<UserEntity>()
                .ToList();
        }

        internal UserEntity GetByName(string userName)
        {
            var kit = SailSqlAccess.UseRunSQL();
            return kit
                .select("*")
                .from("sys_user")
                .where("user_name", userName)
                .queryUnique<UserEntity>();
        }

        internal UserEntity GetById(int id)
            => SailSqlAccess.UseRunSQL().useRepo<UserEntity, int>().GetById(id);
    }
}
