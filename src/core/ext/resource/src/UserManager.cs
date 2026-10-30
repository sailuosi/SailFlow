using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Client;

namespace saiflow.hub.Resource
{
    /// <summary>用户。终态：SailSqlAccess / kit。</summary>
    internal class UserManager
    {
        public UserManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        internal List<UserEntity> GetAll()
        {
            var kit = Sail.UseRunSQL();
            return kit
                .select("*")
                .from("sys_user")
                .orderBy("user_name")
                .query<UserEntity>()
                .ToList();
        }

        internal UserEntity GetByName(string userName)
        {
            var kit = Sail.UseRunSQL();
            return kit
                .select("*")
                .from("sys_user")
                .where("user_name", userName)
                .queryUnique<UserEntity>();
        }

        internal UserEntity GetById(int id)
            => Sail.UseRunSQL().useRepo<UserEntity, int>().GetById(id);
    }
}
