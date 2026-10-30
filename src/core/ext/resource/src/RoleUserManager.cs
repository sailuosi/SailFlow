using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;

namespace saiflow.hub.Resource
{
    /// <summary>角色用户。终态：SailSqlAccess / kit；无 Session。</summary>
    internal class RoleUserManager
    {
        private static int[] ParseRoleIds(string[] idsin)
        {
            if (idsin == null || idsin.Length == 0)
                return Array.Empty<int>();
            try
            {
                return idsin.Select(id => int.Parse(id)).ToArray();
            }
            catch (FormatException)
            {
                throw new ArgumentException("角色Id必须为有效的整数");
            }
        }

        internal List<User> GetUserListByRoleCode(string roleCode)
        {
            var kit = SailSqlAccess.UseRunSQL();
            return kit
                .select("U.id AS UserId, U.user_name AS UserName")
                .from("sys_user U")
                .innerJoin("sys_role_user RU ON U.id = RU.user_id")
                .innerJoin("sys_role R ON R.id = RU.role_id")
                .where("R.role_code", roleCode)
                .orderBy("U.id")
                .query<User>()
                .ToList();
        }

        internal List<RoleUserView> GetRoleUserTree()
        {
            var kit = SailSqlAccess.UseRunSQL();
            return kit
                .select("R.id as RoleId, R.role_name as RoleName, R.role_code as RoleCode, U.id as UserId, U.user_name")
                .from("sys_user U")
                .innerJoin("sys_role_user RU ON U.id = RU.user_id")
                .innerJoin("sys_role R ON R.id = RU.role_id")
                .orderBy("R.id, U.id")
                .query<RoleUserView>()
                .ToList();
        }

        internal UserEntity GetUserEntity(string userId)
            => SailSqlAccess.UseRunSQL().useRepo<UserEntity, int>().GetById(int.Parse(userId));

        internal List<RoleUserView> GetUserByRole(int roleId)
        {
            var kit = SailSqlAccess.UseRunSQL();
            return kit
                .select("R.id AS RoleId, R.role_code AS RoleCode, R.role_name AS RoleName, U.id AS UserId, U.user_name AS UserName")
                .from("sys_role R")
                .innerJoin("sys_role_user RU ON R.id = RU.role_id")
                .innerJoin("sys_user U ON RU.user_id = U.id")
                .where("R.id", roleId)
                .orderBy("R.id, U.id")
                .query<RoleUserView>()
                .ToList();
        }

        internal List<RoleUserView> GetUserByRoleIDs(string[] idsin)
        {
            var roleIds = ParseRoleIds(idsin);
            if (roleIds.Length == 0)
                return new List<RoleUserView>();

            var kit = SailSqlAccess.UseRunSQL();
            return kit
                .select("R.id AS RoleId, R.role_code AS RoleCode, R.role_name AS RoleName, U.id AS UserId, U.user_name AS UserName")
                .from("sys_role R")
                .innerJoin("sys_role_user RU ON R.id = RU.role_id")
                .innerJoin("sys_user U ON RU.user_id = U.id")
                .whereIn("R.id", roleIds)
                .orderBy("R.id, U.id")
                .query<RoleUserView>()
                .ToList();
        }

        internal List<User> GetUserListByRoles(string[] idsin)
        {
            var kit = SailSqlAccess.UseRunSQL();
            return GetUserListByRoles(idsin, kit);
        }

        internal List<User> GetUserListByRoles(string[] idsin, SQLBuilder kit)
        {
            if (idsin == null || !idsin.Any())
                return new List<User>();

            var roleIds = ParseRoleIds(idsin);
            if (roleIds.Length == 0)
                return new List<User>();

            return kit
                .distinct()
                .select("U.id AS UserId, U.user_name AS UserName")
                .from("sys_role R")
                .innerJoin("sys_role_user RU ON R.id = RU.role_id")
                .innerJoin("sys_user U ON RU.user_id = U.id")
                .whereIn("R.id", roleIds)
                .orderBy("U.id")
                .query<User>()
                .ToList();
        }

        internal List<User> GetUserListByRole(string roleId)
        {
            int intRoleId = int.Parse(roleId);
            var kit = SailSqlAccess.UseRunSQL();
            return kit
                .select("U.id AS UserId, U.user_name AS UserName")
                .from("sys_role R")
                .innerJoin("sys_role_user RU ON R.id = RU.role_id")
                .innerJoin("sys_user U ON RU.user_id = U.id")
                .where("R.id", intRoleId)
                .orderBy("R.id, U.id")
                .query<User>()
                .ToList();
        }

        internal IList<Role> GetRoleListByRoleUserView(List<RoleUserView> roleUserView)
        {
            int roleId = 0;
            Role role = null;
            User user = null;
            List<Role> newRoleList = new List<Role>();
            foreach (var item in roleUserView)
            {
                if (item.RoleId != roleId)
                {
                    role = new Role();
                    role.Id = item.RoleId.ToString();
                    role.RoleCode = item.RoleCode;
                    role.RoleName = item.RoleName;
                    role.UserList = new List<User>();
                    user = new User();
                    user.UserId = item.UserId.ToString();
                    user.UserName = item.UserName;
                    role.UserList.Add(user);

                    newRoleList.Add(role);
                    roleId = item.RoleId;
                }
                else
                {
                    user = new User();
                    user.UserId = item.UserId.ToString();
                    user.UserName = item.UserName;
                    role.UserList.Add(user);
                }
            }
            return newRoleList;
        }
    }
}
