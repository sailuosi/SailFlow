using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.SqlProvider;
using saiflow.engine.Client;

namespace saiflow.engine.Business.Manager
{
    /// <summary>
    /// 节点转移实例（wf_transition_instance）。
    /// 终态：无事务从 Client 取 kit；有事务只用传入的同一个 kit。
    /// </summary>
    internal class TransitionInstanceManager
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
                throw new WorkflowException(LocalizeHelper.GetEngineMessage("transitioninstancemanager.entrust.error"));
            return nodeList[0];
        }

        internal TransitionInstanceEntity GetLastTaskTransition(string appName, string appInstanceId, string processId)
        {
            var nodeList = GetWorkItemTransitonInstance(appInstanceId, processId).ToList();
            if (nodeList.Count == 0)
                throw new WorkflowException(LocalizeHelper.GetEngineMessage("transitioninstancemanager.getlasttasktransition.error"));
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
            return kit
                .select("T.*")
                .from($"{SailSqlDialect.TransitionInstanceTable} T")
                .innerJoin($"{SailSqlDialect.ActivityInstanceTable} A ON T.to_activity_instance_id = A.Id")
                .where("T.app_instance_id", appInstanceId)
                .where("T.process_id", processId)
                .where("(T.to_activity_type=4 OR T.to_activity_type=5 OR T.to_activity_type=6 OR A.work_item_type=1)")
                .orderBy("T.created_datetime DESC")
                .query<TransitionInstanceEntity>();
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
            return kit
                .select("*")
                .from(SailSqlDialect.TransitionInstanceTable)
                .where("app_instance_id", appInstanceId)
                .where("process_id", processId)
                .where("to_activity_type", (int)toActivityType)
                .orderBy("id DESC")
                .query<TransitionInstanceEntity>();
        }

        internal IEnumerable<TransitionInstanceEntity> GetTransitionInstanceList(
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
            return kit
                .select("*")
                .from(SailSqlDialect.TransitionInstanceTable)
                .where("app_instance_id", appInstanceId)
                .where("process_id", processId)
                .where("process_instance_id", processInstanceId)
                .orderBy("id DESC")
                .query<TransitionInstanceEntity>();
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
            var transitionList = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.TransitionInstanceTable)
                .where("from_activity_instance_id", fromActivityInstanceId)
                .query<TransitionInstanceEntity>();

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
