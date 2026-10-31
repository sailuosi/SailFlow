using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Business.Entity;
using saiflow.engine.Client;

namespace saiflow.engine.Business.Manager
{
    /// <summary>
    /// 节点转移实例（wf_transition_instance）。
    /// 终态：无事务从 Client 取 kit；有事务只用传入的同一个 kit。
    /// </summary>
    public class TransitionInstanceManager
    {
        public TransitionInstanceManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }
        #region Create
        internal TransitionInstanceEntity CreateTransitionInstanceObject(
            ProcessInstanceEntity processInstance,
            string transitionId,
            ActivityInstanceEntity fromActivityInstance,
            ActivityInstanceEntity toActivityInstance,
            TransitionTypeEnum transitionType,
            TransitionFlyingTypeEnum flyingType,
            WfAppRunner runner,
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
        internal int Insert(TransitionInstanceEntity entity)
        {
            var kit = Sail.UseRunSQL();
            return Insert(kit, entity);
        }

        internal int Insert(SQLBuilder kit, TransitionInstanceEntity entity)
        {
            var newId = kit.useRepo<TransitionInstanceEntity, int>().InsertWithPK(entity);
            entity.Id = newId;
            return newId;
        }

        internal void Delete(int transitionInstanceId)
            => Sail.UseRunSQL().useRepo<TransitionInstanceEntity, int>().DeleteById(transitionInstanceId);

        internal void Delete(SQLBuilder kit, int transitionInstanceId)
            => kit.useRepo<TransitionInstanceEntity, int>().DeleteById(transitionInstanceId);
        #endregion

        #region Query
        internal TransitionInstanceEntity GetById(int transitionInstanceId)
            => Sail.UseRunSQL().useRepo<TransitionInstanceEntity, int>().GetById(transitionInstanceId);

        internal TransitionInstanceEntity GetById(SQLBuilder kit, int transitionInstanceId)
            => kit.useRepo<TransitionInstanceEntity, int>().GetById(transitionInstanceId);

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
            var clip = Sibling(kit).useClip();
            clip.from<TransitionInstanceEntity>(out var t);
            clip.InnerJoin<ActivityInstanceEntity>(out var a).on(() => t.ToActivityInstanceId == a.Id);
            clip.where(() => t.AppInstanceId, appInstanceId);
            clip.where(() => t.ProcessId, processId);
            clip.sinkOR();
            clip.whereIn(() => t.ToActivityType, new[]
            {
                (short)ActivityTypeEnum.TaskNode,
                (short)ActivityTypeEnum.SubProcessNode,
                (short)ActivityTypeEnum.MultiSignNode
            });
            clip.where(() => a.WorkItemType, (short)1);
            clip.rise();
            clip.orderByDesc(() => t.CreatedDateTime);
            return clip.select(t).queryList();
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
            var clip = Sibling(kit).useClip();
            clip.from<TransitionInstanceEntity>(out var t);
            clip.where(() => t.AppInstanceId, appInstanceId);
            clip.where(() => t.ProcessId, processId);
            clip.where(() => t.ToActivityType, (short)toActivityType);
            clip.orderByDesc(() => t.Id);
            return clip.select(t).queryList();
        }

        public IEnumerable<TransitionInstanceEntity> GetTransitionInstanceList(
            string appInstanceId, string processId, string version)
        {
            IEnumerable<TransitionInstanceEntity> list = new List<TransitionInstanceEntity>();
            var pim = new ProcessInstanceManager(Sail);
            var pi = pim.GetProcessInstanceCurrent(appInstanceId, processId, version);
            if (pi != null)
                list = GetTransitionInstanceList(appInstanceId, processId, pi.Id);
            return list;
        }

        internal IEnumerable<TransitionInstanceEntity> GetTransitionInstanceList(
            string appInstanceId, string processId, int processInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTransitionInstanceList(kit, appInstanceId, processId, processInstanceId);
        }

        internal IEnumerable<TransitionInstanceEntity> GetTransitionInstanceList(
            SQLBuilder kit, string appInstanceId, string processId, int processInstanceId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<TransitionInstanceEntity>(out var t);
            clip.where(() => t.AppInstanceId, appInstanceId);
            clip.where(() => t.ProcessId, processId);
            clip.where(() => t.ProcessInstanceId, processInstanceId);
            clip.orderByDesc(() => t.Id);
            return clip.select(t).queryList();
        }

        internal IList<ActivityInstanceEntity> GetTargetActivityInstanceList(int fromActivityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetTargetActivityInstanceList(kit, fromActivityInstanceId);
        }

        internal IList<ActivityInstanceEntity> GetTargetActivityInstanceList(SQLBuilder kit, int fromActivityInstanceId)
        {
            var targetIDs = GetTargetActivityInstanceIDs(kit, fromActivityInstanceId);
            return Sibling(kit).useRepo<ActivityInstanceEntity>()
                .GetByIds(targetIDs)
                .ToList();
        }

        /// <summary>同库同事务的新 kit，避免链式 select 状态互相污染。</summary>
        private static SQLBuilder Sibling(SQLBuilder kit)
        {
            var next = kit.DBLive.useSQL();
            if (kit.Executor != null)
                next.useTransaction(kit.Executor);
            return next;
        }

        private static IList<dynamic> GetTargetActivityInstanceIDs(SQLBuilder kit, int fromActivityInstanceId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<TransitionInstanceEntity>(out var t);
            clip.where(() => t.FromActivityInstanceId, fromActivityInstanceId);
            var transitionList = clip.select(t).queryList().ToList();

            var targetIDs = new List<dynamic>();
            foreach (var trans in transitionList)
            {
                if (trans.ToActivityType == (int)ActivityTypeEnum.GatewayNode)
                    targetIDs.AddRange(GetTargetActivityInstanceIDs(kit, trans.ToActivityInstanceId));
                else
                    targetIDs.Add(trans.ToActivityInstanceId);
            }
            return targetIDs;
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
