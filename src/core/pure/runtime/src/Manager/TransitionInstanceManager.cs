using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.hub.Localize;
using sailflow.utils;
using sailflow.hosting;
using sailflow.data.HotShard;
using sailflow.log;
using sailflow.run;
using sailflow.storage;

namespace sailflow.data
{
    /// <summary>
    /// 节点转移轨迹（<see cref="TrackEntity"/> / <c>sf_track</c> / 白名单热分表）。
    /// 终态：无事务从 Client 取 kit；有事务只用传入的同一个 kit。
    /// </summary>
    public class TransitionInstanceManager : ITransitionInstanceManager
    {
        public TransitionInstanceManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        protected ITransitionInstanceStorage Storage =>
            Sail.Storages.GetRequired<ITransitionInstanceStorage>();

        Exception FailThrow(Exception ex, string op, string detail = null)
        {
            SailFailLog.Exception(Sail, SailLogCategories.Engine, "Transition." + op, ex, detail);
            return ex;
        }

        ISailHotShardRouter Router => Sail.HotShard;

        #region Create
        public TrackEntity CreateTransitionInstanceObject(
            ProcessInstanceEntity processInstance,
            string transitionId,
            ActivityInstanceEntity fromActivityInstance,
            ActivityInstanceEntity toActivityInstance,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            FlowCommandContext runner,
            byte conditionParseResult)
        {
            var entity = new TrackEntity
            {
                BizChannel = processInstance.BizChannel,
                BizPK = processInstance.BizPK,
                ProcessId = processInstance.ProcessId,
                ProcessInstanceId = processInstance.ProcessInstanceId,
                TransitionId = transitionId,
                TransitionType = (byte)transitionType,
                FlyingType = (byte)flyingType,
                FromActId = fromActivityInstance.ActivityId,
                FromActInstanceId = fromActivityInstance.ActivityInstanceId,
                FromActType = fromActivityInstance.ActivityType,
                FromActName = fromActivityInstance.ActivityName,
                ToActId = toActivityInstance.ActivityId,
                ToActInstanceId = toActivityInstance.ActivityInstanceId,
                ToActType = toActivityInstance.ActivityType,
                ToActName = toActivityInstance.ActivityName,
                ConditionParsedResult = conditionParseResult,
                CreatedUserId = runner.UserId,
                CreatedUserName = runner.UserName,
                CreatedDateTime = DateTime.UtcNow
            };
            return entity;
        }
        #endregion

        #region Insert / Delete

        public string Insert(SQLBuilder kit, TrackEntity entity)
        {
            var table = Router.ResolveTransitionTable(entity?.ProcessId);
            return Storage.Insert(kit, entity, table);
        }

        #endregion

        #region Query


        public TrackEntity GetEndTransition(string bizChannel, string bizPK, string processId)
        {
            var nodeList = GetTransitonInstance(bizPK, processId, ActivityTypeEnum.EndNode).ToList();
            if (nodeList == null || nodeList.Count == 0)
                throw FailThrow(new WorkflowException(
                    Sail.Messages.GetEngineMessage("transitioninstancemanager.entrust.error")), "GetEndTransition");
            return nodeList[0];
        }

        public TrackEntity GetLastTaskTransition(string bizChannel, string bizPK, string processId)
        {
            var nodeList = GetWorkItemTransitonInstance(bizPK, processId).ToList();
            if (nodeList.Count == 0)
                throw FailThrow(new WorkflowException(
                    Sail.Messages.GetEngineMessage("transitioninstancemanager.getlasttasktransition.error")), "GetLastTaskTransition");
            return nodeList[0];
        }

        public IEnumerable<TrackEntity> GetWorkItemTransitonInstance(string bizPK, string processId)
        {
            var kit = Sail.UseRunSQL();
            return GetWorkItemTransitonInstance(kit, bizPK, processId);
        }

        public IEnumerable<TrackEntity> GetWorkItemTransitonInstance(
            SQLBuilder kit, string bizPK, string processId)
        {
            return QueryWithFallback(kit, processId,
                table => Storage.ListWorkItemByBiz(kit, table, bizPK, processId)?.ToList()
                         ?? new List<TrackEntity>());
        }

        public IEnumerable<TrackEntity> GetTransitonInstance(
            string bizPK, string processId, ActivityTypeEnum toActivityType)
        {
            var kit = Sail.UseRunSQL();
            return GetTransitonInstance(kit, bizPK, processId, toActivityType);
        }

        public IEnumerable<TrackEntity> GetTransitonInstance(
            SQLBuilder kit, string bizPK, string processId, ActivityTypeEnum toActivityType)
        {
            return QueryWithFallback(kit, processId,
                table => Storage.ListByBizAndToActType(
                             kit, table, bizPK, processId, (short)toActivityType)?.ToList()
                         ?? new List<TrackEntity>());
        }

        public IEnumerable<TrackEntity> GetTransitionInstanceList(
            string bizPK, string processId, string version)
        {
            IEnumerable<TrackEntity> list = new List<TrackEntity>();
            var pim = Sail.ProcessInstances;
            var pi = pim.GetProcessInstanceCurrent(bizPK, processId, version);
            if (pi != null)
                list = GetTransitionInstanceListByProcessInstance(bizPK, processId, pi.ProcessInstanceId);
            return list;
        }

        public IEnumerable<TrackEntity> GetTransitionInstanceListByProcessInstance(
            string bizPK, string processId, string processInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTransitionInstanceListByProcessInstance(kit, bizPK, processId, processInstanceId);
        }

        public IEnumerable<TrackEntity> GetTransitionInstanceListByProcessInstance(
            SQLBuilder kit, string bizPK, string processId, string processInstanceId)
        {
            return QueryWithFallback(kit, processId,
                table => Storage.ListByProcessInstance(
                             kit, table, bizPK, processId, processInstanceId)?.ToList()
                         ?? new List<TrackEntity>());
        }

        public IList<ActivityInstanceEntity> GetTargetActivityInstanceList(string fromActivityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTargetActivityInstanceList(kit, fromActivityInstanceId);
        }

        public IList<ActivityInstanceEntity> GetTargetActivityInstanceList(SQLBuilder kit, string fromActivityInstanceId)
        {
            var targetIDs = GetTargetActivityInstanceIDs(kit, fromActivityInstanceId);
            var ids = new List<string>();
            if (targetIDs != null)
            {
                foreach (var x in targetIDs)
                {
                    var s = x?.ToString();
                    if (!string.IsNullOrWhiteSpace(s))
                        ids.Add(s);
                }
            }

            return Sail.Storages.GetRequired<IActivityInstanceStorage>().ListByIds(kit, ids);
        }

        /// <summary>同库同事务的新 kit，避免链式 select 状态互相污染。</summary>
        private IList<dynamic> GetTargetActivityInstanceIDs(SQLBuilder kit, string fromActivityInstanceId)
        {
            var processId = ResolveProcessIdFromActivity(kit, fromActivityInstanceId);
            var transitions = QueryWithFallback(kit, processId,
                table => Storage.ListByFromActivityInstance(kit, table, fromActivityInstanceId)?.ToList()
                         ?? new List<TrackEntity>());

            var targetIDs = new List<dynamic>();
            foreach (var trans in transitions)
            {
                if (trans.ToActType == (int)ActivityTypeEnum.GatewayNode)
                    targetIDs.AddRange(GetTargetActivityInstanceIDs(kit, trans.ToActInstanceId));
                else
                    targetIDs.Add(trans.ToActInstanceId);
            }
            return targetIDs;
        }

        private string ResolveProcessIdFromActivity(SQLBuilder kit, string activityInstanceId)
        {
            return Sail.Storages.GetRequired<IActivityInstanceStorage>()
                .GetProcessId(kit, activityInstanceId);
        }

        /// <summary>
        /// 白名单分表优先；再合并默认表同条件行（启用瞬间旧数据仍在默认表）。
        /// </summary>
        private List<TrackEntity> QueryWithFallback(
            SQLBuilder kit,
            string processId,
            Func<string, List<TrackEntity>> query)
        {
            var primary = Router.ResolveTransitionTable(processId);
            List<TrackEntity> rows;
            try
            {
                rows = query(primary) ?? new List<TrackEntity>();
            }
            catch
            {
                rows = new List<TrackEntity>();
            }

            if (!Router.IsSharded(processId))
                return rows;

            var def = Router.DefaultTransitionTable;
            if (string.Equals(primary, def, StringComparison.OrdinalIgnoreCase))
                return rows;

            List<TrackEntity> legacy;
            try
            {
                legacy = query(def) ?? new List<TrackEntity>();
            }
            catch
            {
                return rows;
            }

            if (legacy.Count == 0)
                return rows;

            var seen = new HashSet<string>(rows.Where(x => !string.IsNullOrWhiteSpace(x.TrackId)).Select(x => x.TrackId));
            foreach (var row in legacy)
            {
                if (!string.IsNullOrWhiteSpace(row.TrackId) && seen.Contains(row.TrackId))
                    continue;
                rows.Add(row);
            }
            return rows;
        }

        #endregion
    }
}
