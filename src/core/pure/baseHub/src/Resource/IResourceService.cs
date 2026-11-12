using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sailflow.hub.Resource
{
    /// <summary>
    /// Resource Service Interface
    /// 资源/组织人员契约：引擎不绑定宿主组织表；由宿主 UseXxx 具化后经 <see cref="ResourceServiceLocator"/> 注册。
    /// </summary>
    public interface IResourceService
    {
        IList<Role> GetRoleAll();
        IList<User> GetUserListByRoles(string[] roleIds);
        IList<Role> FillUsersIntoRoles(string[] roleIds);
        IList<User> GetUserAll();
        User GetUserById(string id);
        IList<User> GetUserListByRole(string roleId);
        IList<User> GetUserListByRoleCode(string roleCode);
        IList<User> GetUserListByRoleReceiverType(string[] roleIds, string curUserId, int receiverType);
        Role GetRoleByCode(string roleCode);
    }
}
