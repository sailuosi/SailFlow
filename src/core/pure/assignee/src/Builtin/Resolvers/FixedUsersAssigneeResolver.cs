using System.Globalization;
using saiflow.engine.Common;

namespace saiflow.assignee.Builtin;

/// <summary>固定人：Params.users = [{userId,userName,groupId?,groupRole?}, ...] 或 userIds 逗号串。禁止 sealed。</summary>
public class FixedUsersAssigneeResolver : IAssigneeResolver
{
    public virtual string Type => AssigneeResolveTypeCodes.FixedUsers;

    public virtual PerformerList Resolve(AssigneeResolveContext ctx)
    {
        var list = new PerformerList();
        var config = ctx.Config;
        if (config?.Params != null && config.Params.TryGetValue("users", out var usersRaw) && usersRaw != null)
        {
            if (usersRaw is IEnumerable<object> rows)
            {
                foreach (var row in rows)
                    TryAddFromObject(list, row);
            }
        }

        if (list.Count == 0)
        {
            var ids = ConfigPairParams.GetStringArray(config, "userIds");
            var names = ConfigPairParams.GetStringArray(config, "userNames");
            for (var i = 0; i < ids.Length; i++)
            {
                var name = i < names.Length ? names[i] : ids[i];
                list.Add(new Performer(ids[i], name) { GroupRole = GroupRoles.Member });
            }
        }

        return list;
    }

    protected virtual void TryAddFromObject(PerformerList list, object row)
    {
        if (row is Performer p)
        {
            list.Add(p);
            return;
        }

        if (row is IDictionary<string, object> dict)
        {
            var id = DictStr(dict, "userId") ?? DictStr(dict, "UserId");
            if (string.IsNullOrEmpty(id))
                return;
            var name = DictStr(dict, "userName") ?? DictStr(dict, "UserName") ?? id;
            list.Add(new Performer(id, name)
            {
                GroupId = DictStr(dict, "groupId") ?? DictStr(dict, "GroupId"),
                GroupName = DictStr(dict, "groupName") ?? DictStr(dict, "GroupName"),
                GroupRole = DictStr(dict, "groupRole") ?? DictStr(dict, "GroupRole") ?? GroupRoles.Member,
                UnitId = DictStr(dict, "unitId") ?? DictStr(dict, "UnitId"),
                UnitCode = DictStr(dict, "unitCode") ?? DictStr(dict, "UnitCode"),
                UnitName = DictStr(dict, "unitName") ?? DictStr(dict, "UnitName")
            });
            return;
        }

        // JSON 反序列化常见为 Dictionary<string, object?> / Expando
        if (row is System.Collections.IDictionary idict)
        {
            string? Get(string k)
            {
                foreach (System.Collections.DictionaryEntry e in idict)
                {
                    if (string.Equals(Convert.ToString(e.Key), k, StringComparison.OrdinalIgnoreCase))
                        return Convert.ToString(e.Value, CultureInfo.InvariantCulture);
                }
                return null;
            }

            var id = Get("userId") ?? Get("UserId");
            if (string.IsNullOrEmpty(id))
                return;
            list.Add(new Performer(id, Get("userName") ?? Get("UserName") ?? id)
            {
                GroupId = Get("groupId") ?? Get("GroupId"),
                GroupName = Get("groupName") ?? Get("GroupName"),
                GroupRole = Get("groupRole") ?? Get("GroupRole") ?? GroupRoles.Member,
                UnitId = Get("unitId") ?? Get("UnitId"),
                UnitCode = Get("unitCode") ?? Get("UnitCode"),
                UnitName = Get("unitName") ?? Get("UnitName")
            });
        }
    }

    private static string? DictStr(IDictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var v) || v == null)
            return null;
        return Convert.ToString(v, CultureInfo.InvariantCulture);
    }
}
