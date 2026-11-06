using System.Collections.Generic;

namespace saiflow.hub.Resource
{
    /// <summary>
    /// Department / org-rank query contract.
    /// 部门职级查询契约：引擎不内置组织人员表；由宿主实现并经 <see cref="DeptServiceLocator"/> 注册。
    /// </summary>
    public interface IDeptService
    {
        /// <summary>
        /// Filter role users by receiver rank relative to <paramref name="curUserId"/>.
        /// receiverType: 1 Superior / 2 Compeer / 3 Subordinate.
        /// </summary>
        IList<User> GetUserListByDeptRank(string[] roleIDs, string curUserId, int receiverType);
    }
}
