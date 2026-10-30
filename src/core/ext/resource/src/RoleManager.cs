using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Client;

namespace saiflow.hub.Resource
{
    /// <summary>角色。终态：SailSqlAccess / kit。</summary>
    internal class RoleManager
    {
        public RoleManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        internal List<RoleEntity> GetAll()
        {
            var kit = Sail.UseRunSQL();
            return kit
                .select("*")
                .from("sys_role")
                .orderBy("role_name")
                .query<RoleEntity>()
                .ToList();
        }

        internal RoleEntity GetByCode(string roleCode)
        {
            var kit = Sail.UseRunSQL();
            return kit
                .select("*")
                .from("sys_role")
                .where("role_code", roleCode)
                .queryUnique<RoleEntity>();
        }
    }
}
