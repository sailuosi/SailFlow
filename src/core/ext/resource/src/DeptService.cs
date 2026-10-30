using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Client;

namespace saiflow.hub.Resource
{
    /// <summary>
    /// Department Service Interface
    /// 部门组织机构接口
    /// </summary>
    public interface IDeptService
    {
        IList<User> GetUserListByDeptRank(string[] roleIDs, string curUserId, int receiverType);
    }

    /// <summary>
    /// Department Organizational Structure Creation Factory Category
    /// 部门组织机构创建工厂类
    /// </summary>
    public class DeptServiceFactory
    {
        /// <summary>
        /// Method of creation
        /// Users can extend the implementation class based on their organizational data
        /// Two types of queries have been implemented here“
        /// 1) Table query
        /// 2) Query of stored procedures
        /// 3) Other extensions
        /// </summary>
        public static IDeptService CreateDeptService(SailClient client)
        {
            IDeptService deptService = new DeptServiceQuery(client);
            //IDeptService deptService = new DeptServiceSP(client);
            return deptService;
        }
    }

    /// <summary>
    /// Implementing user level queries using SQL table statements
    /// SQL表语句实现用户级别查询
    /// </summary>
    public class DeptServiceQuery : IDeptService
    {
        public DeptServiceQuery(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        private SQLBuilder Kit() => Sail.UseRunSQL();

        private static int[] ParseRoleIds(string[] roleIds)
        {
            if (roleIds == null || roleIds.Length == 0)
                return Array.Empty<int>();
            return roleIds.Select(id => int.Parse(id)).ToArray();
        }

        /// <summary>
        /// Retrieve data from the role user tree based on the recipient type
        /// </summary>
        public IList<User> GetUserListByDeptRank(string[] roleIds, string curUserId, int receiverType)
        {
            if (receiverType == 1) return GetSuperiorList(roleIds, int.Parse(curUserId));
            else if (receiverType == 2) return GetCompeerList(roleIds, int.Parse(curUserId));
            else if (receiverType == 3) return GetSubordinateList(roleIds, int.Parse(curUserId));
            else return null;
        }

        /// <summary>
        /// Get the list of superiors
        /// 获取上司列表
        /// </summary>
        private IList<User> GetSuperiorList(string[] roleIds, int curUserId)
        {
            var ids = ParseRoleIds(roleIds);
            return Kit()
                .select("U.id AS UserId, U.user_name AS UserName")
                .from("sys_user U")
                .innerJoin("sys_employee_manager EM ON U.id = EM.manager_user_id")
                .innerJoin("sys_role_user RU ON U.id = RU.user_id")
                .whereIn("RU.role_id", ids)
                .where("EM.employee_user_id", curUserId)
                .query<User>()
                .ToList();
        }

        /// <summary>
        /// Get colleague list
        /// 获取同事列表
        /// </summary>
        private IList<User> GetCompeerList(string[] roleIds, int curUserId)
        {
            var ids = ParseRoleIds(roleIds);
            return Kit()
                .select("U.id AS UserId, U.user_name AS UserName")
                .from("sys_user U")
                .innerJoin("sys_employee_manager EM ON U.id = EM.employee_user_id")
                .innerJoin("sys_role_user RU ON U.id = RU.user_id")
                .whereIn("RU.role_id", ids)
                .whereIn("EM.manager_user_id", sub => sub
                    .select("manager_user_id")
                    .from("sys_employee_manager")
                    .where("employee_user_id", curUserId))
                .query<User>()
                .ToList();
        }

        /// <summary>
        /// Get the list of subordinates
        /// 获取下属列表
        /// </summary>
        private IList<User> GetSubordinateList(string[] roleIds, int curUserId)
        {
            var ids = ParseRoleIds(roleIds);
            return Kit()
                .select("U.id AS UserId, U.user_name AS UserName")
                .from("sys_user U")
                .innerJoin("sys_employee_manager EM ON U.id = EM.employee_user_id")
                .innerJoin("sys_role_user RU ON U.id = RU.user_id")
                .whereIn("RU.role_id", ids)
                .where("EM.manager_user_id", curUserId)
                .query<User>()
                .ToList();
        }
    }

    /// <summary>
    /// Implementing user level queries through stored procedures
    /// 存储过程实现用户级别查询（F2-f 保留 Proc；无 CreateConnection / 无 ManagerBase）
    /// </summary>
    public class DeptServiceSP : IDeptService
    {
        /// <summary>
        /// Retrieve data from the role user tree based on the recipient type
        /// </summary>
        public IList<User> GetUserListByDeptRank(string[] roleIds, string curUserId, int receiverType)
        {
            var param = new Dictionary<string, object>
            {
                ["@roleIds"] = string.Join(",", roleIds),
                ["@curUserId"] = int.Parse(curUserId),
                ["@receiverType"] = receiverType
            };

            var receiverList = SailCmdRunner.QueryEntities<User>(
                "pr_sys_DeptUserListRankQuery",
                param,
                CommandType.StoredProcedure);

            if (receiverList != null && receiverList.Count > 0)
                return receiverList;

            return null;
        }
    }
}
