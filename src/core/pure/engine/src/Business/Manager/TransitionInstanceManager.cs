using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Entity;
using saiflow.engine.Client;
using saiflow.engine.HotShard;

namespace saiflow.engine.Manager
{
    /// <summary>
    /// 节点转移实例（wf_transition_instance / 白名单热分表）。
    /// 终态：无事务从 Client 取 kit；有事务只用传入的同一个 kit。
    /// </summary>
    public class TransitionInstanceManager
    {
        public TransitionInstanceManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        ISailHotShardRouter Router => Sail.HotShard;

        #region Create
        internal TransitionInstanceEntity CreateTransitionInstanceObject(
            ProcessInstanceEntity processInstance,
            string transitionId,
            ActivityInstanceEntity fromActivityInstance,
            ActivityInstanceEntity toActivityInstance,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            FlowCommandContext runner,
            byte conditionParseResult)
        {
            var entity = new TransitionInstanceEntity
            {
                AppName = processInstance.AppName,
                AppInstanceId = processInstance.AppInstanceId,
                ProcessId = processInstance.ProcessId,
                ProcessInstanceId = processInstance.Id,
                TransitionId = transitionId,
                TransitionType = (byte)transitionType,
                FlyingType = (byte)flyingType,
                FromActivityId = fromActivityInstance.ActivityId,
                FromActivityInstanceId = fromActivityInstance.Id,
                FromActivityType = fromActivityInstance.ActivityType,
                FromActivityName = fromActivityInstance.ActivityName,
                ToActivityId = toActivityInstance.ActivityId,
                ToActivityInstanceId = toActivityInstance.Id,
                ToActivityType = toActivityInstance.ActivityType,
                ToActivityName = toActivityInstance.ActivityName,
                ConditionParsedResult = conditionParseResult,
                CreatedUserId = runner.UserId,
                CreatedUserName = runner.UserName,
                CreatedDateTime = DateTime.UtcNow
            };
            return entity;
        }
        #endregion

        #region Insert / Delete
        internal string Insert(TransitionInstanceEntity entity)
        {
            var kit = Sail.UseRunSQL();
            return Insert(kit, entity);
        }

        internal string Insert(SQLBuilder kit, TransitionInstanceEntity entity)
        {
            var table = Router.ResolveTransitionTable(entity?.ProcessId);
            var newId = HotShardEntityAccess.InsertReturnId(kit, table, entity);
            entity.Id = newId;
            return newId;
        }

        internal void Delete(string transitionInstanceId)
            => Delete(Sail.UseRunSQL(), transitionInstanceId);

        internal void Delete(SQLBuilder kit, string transitionInstanceId)
            => HotShardEntityAccess.DeleteByIdProbing<TransitionInstanceEntity>(
                kit, Router.TransitionLookupTables, transitionInstanceId);
        #endregion

        #region Query
        internal TransitionInstanceEntity GetById(string transitionInstanceId)
            => GetById(Sail.UseRunSQL(), transitionInstanceId);

        internal TransitionInstanceEntity GetById(SQLBuilder kit, string transitionInstanceId)
            => HotShardEntityAccess.GetByIdProbing<TransitionInstanceEntity>(
                kit, Router.TransitionLookupTables, transitionInstanceId);

        internal TransitionInstanceEntity GetEndTransition(string appName, string appInstanceId, string processId)
        {
            var nodeList = GetTransitonInstance(appInstanceId, processId, ActivityTypeEnum.EndNode).ToList();
            if (nodeList == null || nodeList.Count == 0)
                throw new WorkflowException(Sail.Messages.GetEngineMessage("transitioninstancemanager.entrust.error"));
            return nodeList[0];
        }

        internal TransitionInstanceEntity GetLastTaskTransition(string appName, string appInstanceId, string processId)
        {
            var nodeList = GetWorkItemTransitonInstance(appInstanceId, processId).ToList();
            if (nodeList.Count == 0)
                throw new WorkflowException(Sail.Messages.GetEngineMessage("transitioninstancemanager.getlasttasktransition.error"));
            return nodeList[0];
        }

        internal IEnumerable<TransitionInstanceEntity> GetWorkItemTransitonInstance(string appInstanceId, string processId)
        {
            var kit = Sail.UseRunSQL();
            return GetWorkItemTransitonInstance(kit, appInstanceId, processId);
        }

        internal IEnumerable<TransitionInstanceEntity> GetWorkItemTransitonInstance(
            SQLBuilder kit, string appInstanceId, string processId)
        {
            return QueryWithFallback(kit, processId, table =>
            {
                var clip = kit.useSQL().useClip();
                clip.from<TransitionInstanceEntity>(table, out var t);
                clip.InnerJoin<ActivityInstanceEntity>(out var a).on(() => t.ToActivityInstanceId == a.Id);
                clip.where(() => t.AppInstanceId, appInstanceId);
                clip.where(() => t.ProcessId, processId);
                clip.sinkOR();
                clip.whereIn(() => t.ToActivityType, new short?[]
                {
                    (short)ActivityTypeEnum.TaskNode,
                    (short)ActivityTypeEnum.SubProcessNode,
                    (short)ActivityTypeEnum.MultiSignNode
                });
                clip.where(() => a.WorkItemType, (short)1);
                clip.rise();
                clip.orderByDesc(() => t.CreatedDateTime);
                return clip.select(t).queryList().ToList();
            });
        }

        internal IEnumerable<TransitionInstanceEntity> GetTransitonInstance(
            string appInstanceId, string processId, ActivityTypeEnum toActivityType)
        {
            var kit = Sail.UseRunSQL();
            return GetTransitonInstance(kit, appInstanceId, processId, toActivityType);
        }

        internal IEnumerable<TransitionInstanceEntity> GetTransitonInstance(
            SQLBuilder kit, string appInstanceId, string processId, ActivityTypeEnum toActivityType)
        {
            return QueryWithFallback(kit, processId, table =>
            {
                var clip = kit.useSQL().useClip();
                clip.from<TransitionInstanceEntity>(table, out var t);
                clip.where(() => t.AppInstanceId, appInstanceId);
                clip.where(() => t.ProcessId, processId);
                clip.where(() => t.ToActivityType, (short)toActivityType);
                clip.orderByDesc(() => t.Id);
                return clip.select(t).queryList().ToList();
            });
        }

        public IEnumerable<TransitionInstanceEntity> GetTransitionInstanceList(
            string appInstanceId, string processId, string version)
        {
            IEnumerable<TransitionInstanceEntity> list = new List<TransitionInstanceEntity>();
            var pim = Sail.ProcessInstances;
            var pi = pim.GetProcessInstanceCurrent(appInstanceId, processId, version);
            if (pi != null)
                list = GetTransitionInstanceListByProcessInstance(appInstanceId, processId, pi.Id);
            return list;
        }

        internal IEnumerable<TransitionInstanceEntity> GetTransitionInstanceListByProcessInstance(
            string appInstanceId, string processId, string processInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTransitionInstanceListByProcessInstance(kit, appInstanceId, processId, processInstanceId);
        }

        internal IEnumerable<TransitionInstanceEntity> GetTransitionInstanceListByProcessInstance(
            SQLBuilder kit, string appInstanceId, string processId, string processInstanceId)
        {
            return QueryWithFallback(kit, processId, table =>
            {
                var clip = kit.useSQL().useClip();
                clip.from<TransitionInstanceEntity>(table, out var t);
                clip.where(() => t.AppInstanceId, appInstanceId);
                clip.where(() => t.ProcessId, processId);
                clip.where(() => t.ProcessInstanceId, processInstanceId);
                clip.orderByDesc(() => t.Id);
                return clip.select(t).queryList().ToList();
            });
        }

        internal IList<ActivityInstanceEntity> GetTargetActivityInstanceList(string fromActivityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTargetActivityInstanceList(kit, fromActivityInstanceId);
        }

        internal IList<ActivityInstanceEntity> GetTargetActivityInstanceList(SQLBuilder kit, string fromActivityInstanceId)
        {
            var targetIDs = GetTargetActivityInstanceIDs(kit, fromActivityInstanceId);
            return kit.useSQL().useRepo<ActivityInstanceEntity>()
                .GetByIds(targetIDs)
                .ToList();
        }

        /// <summary>同库同事务的新 kit，避免链式 select 状态互相污染。</summary>
        private IList<dynamic> GetTargetActivityInstanceIDs(SQLBuilder kit, string fromActivityInstanceId)
        {
            var processId = ResolveProcessIdFromActivity(kit, fromActivityInstanceId);
            var transitions = QueryWithFallback(kit, processId, table =>
            {
                var clip = kit.useSQL().useClip();
                clip.from<TransitionInstanceEntity>(table, out var t);
                clip.where(() => t.FromActivityInstanceId, fromActivityInstanceId);
                return clip.select(t).queryList().ToList();
            });

            var targetIDs = new List<dynamic>();
            foreach (var trans in transitions)
            {
                if (trans.ToActivityType == (int)ActivityTypeEnum.GatewayNode)
                    targetIDs.AddRange(GetTargetActivityInstanceIDs(kit, trans.ToActivityInstanceId));
                else
                    targetIDs.Add(trans.ToActivityInstanceId);
            }
            return targetIDs;
        }

        private string ResolveProcessIdFromActivity(SQLBuilder kit, string activityInstanceId)
        {
            try
            {
                var ai = kit.useSQL().useRepo<ActivityInstanceEntity, string>().GetById(activityInstanceId);
                return ai?.ProcessId;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 白名单分表优先；再合并默认表同条件行（启用瞬间旧数据仍在默认表）。
        /// </summary>
        private List<TransitionInstanceEntity> QueryWithFallback(
            SQLBuilder kit,
            string processId,
            Func<string, List<TransitionInstanceEntity>> query)
        {
            var primary = Router.ResolveTransitionTable(processId);
            List<TransitionInstanceEntity> rows;
            try
            {
                rows = query(primary) ?? new List<TransitionInstanceEntity>();
            }
            catch
            {
                rows = new List<TransitionInstanceEntity>();
            }

            if (!Router.IsSharded(processId))
                return rows;

            var def = Router.DefaultTransitionTable;
            if (string.Equals(primary, def, StringComparison.OrdinalIgnoreCase))
                return rows;

            List<TransitionInstanceEntity> legacy;
            try
            {
                legacy = query(def) ?? new List<TransitionInstanceEntity>();
            }
            catch
            {
                return rows;
            }

            if (legacy.Count == 0)
                return rows;

            var seen = new HashSet<string>(rows.Where(x => !string.IsNullOrWhiteSpace(x.Id)).Select(x => x.Id));
            foreach (var row in legacy)
            {
                if (!string.IsNullOrWhiteSpace(row.Id) && seen.Contains(row.Id))
                    continue;
                rows.Add(row);
            }
            return rows;
        }

        internal bool IsTransiionInstancedAndConditionParsedOK(
            string transitionId,
            IList<TransitionInstanceEntity> transitionInstanceList)
        {
            foreach (var transitionInstance in transitionInstanceList)
            {
                if (transitionId == transitionInstance.TransitionId
                    && transitionInstance.ConditionParsedResult == (byte)ConditionParseResultEnum.Passed)
                    return true;
            }
            return false;
        }
        #endregion
    }
}
