using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;

namespace saiflow.hub.Resource
{
    /// <summary>角色。终态：SailSqlAccess / kit。</summary>
    internal class RoleManager
    {
        internal List<RoleEntity> GetAll()
        {
            var kit = SailSqlAccess.UseRunSQL();
            return kit
                .select("*")
                .from("sys_role")
                .orderBy("role_name")
                .query<RoleEntity>()
                .ToList();
        }

        internal RoleEntity GetByCode(string roleCode)
        {
            var kit = SailSqlAccess.UseRunSQL();
            return kit
                .select("*")
                .from("sys_role")
                .where("role_code", roleCode)
                .queryUnique<RoleEntity>();
        }
    }
}
