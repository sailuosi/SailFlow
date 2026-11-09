using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
//using System.Diagnostics;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Form;
using saiflow.hub.Resource;
using saiflow.engine.Utility;
using saiflow.engine.Common;
using saiflow.engine.Entity;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Xpdl
{
    /// <summary>
    /// Process Model Interface
    /// </summary>
    public interface IProcessModel 
    {
        Process Process { get; }
        ProcessEntity ProcessEntity { get; set; }
        saiflow.engine.Xpdl.Activity GetStartActivity();
        saiflow.engine.Xpdl.Activity GetFirstActivity();
        NextActivityMatchedResult GetFirstActivityList(saiflow.engine.Xpdl.Activity startActivity, IDictionary<string, string> conditionKeyValuePair);
        saiflow.engine.Xpdl.Activity GetEndActivity();
        saiflow.engine.Xpdl.Activity GetActivity(string activityId);
        IList<saiflow.engine.Xpdl.Activity> GetActivityList();
        IList<string> GetActivityOutputVarialbeNameList(saiflow.engine.Xpdl.Activity activity);
        saiflow.engine.Xpdl.Activity GetNextActivity(string activityId);
        /// <summary>
        /// Structural routing API: get the next activity tree purely from the process model.
        /// Does not include runtime user resolution. For business/UI usage, prefer WorkflowService.GetNextStepInfo.
        /// 结构层路由接口：仅根据流程模型计算下一步活动树（不包含实际用户列表）。
        /// 业务/界面层请优先使用 WorkflowService.GetNextStepInfo。
        /// </summary>
        IList<NodeView> GetNextActivityTreeView(string currentActivityId);

        /// <summary>
        /// Structural routing API with condition variables. See remarks on GetNextActivityTree(string).
        /// 带条件变量的结构层路由接口，含义同 GetNextActivityTree(string)。
        /// </summary>
        IList<NodeView> GetNextActivityTreeView(string currentActivityId, IDictionary<string, string> conditions);
        /// <summary>
        /// Structural routing API for the beginning of the process, starting from the start activity.
        /// 结构层路由接口：从开始活动起点计算“第一步”可达的活动树。
        /// </summary>
        IList<NodeView> GetFirstActivityTreeView(saiflow.engine.Xpdl.Activity startActivity, IDictionary<string, string> conditions);
        NextActivityListViewResult GetNextActivityListView(string currentActivityId,
            string activityInstanceId,
            IDictionary<string, string> conditions,
            SQLBuilder kit);
        /// <summary>
        /// Get the next activity list with runtime conditions and resource-based filtering.
        /// 注意：该方法仅用于引擎内部（如回退、特殊测试场景）的“结构 + 资源表达式”过滤，
        /// 对于常规下一步查询，推荐通过 WorkflowService.GetNextStepInfo / NextStepParser 使用统一入口。
        /// </summary>
        NextActivityMatchedResult GetNextActivityTreeListRuntime(string currentActivityId,
            string activityInstanceId,
            IDictionary<string, string> conditionKeyValuePair,
            ActivityResource activityResource,
            Expression<Func<ActivityResource, saiflow.engine.Xpdl.Activity, bool>> expression,
            SQLBuilder kit);
        IList<saiflow.engine.Xpdl.Activity> GetNextActivityListWithoutCondition(string activityId);

        IList<NodeView> GetPreviousActivityTree(string currentActivityId);
        IList<saiflow.engine.Xpdl.Activity> GetPreviousActivityList(string currentActivityId);
        IList<saiflow.engine.Xpdl.Activity> GetPreviousActivityList(string currentActivityId, out bool hasGatewayPassed);
        IList<saiflow.engine.Xpdl.Activity> GetFromActivityList(string toActivityId);

        saiflow.engine.Xpdl.Activity GetBackwardGatewayActivity(saiflow.engine.Xpdl.Activity gatewayActivity, 
            ref int joinCount, ref int splitCount);
        Int32 GetBackwardTransitionListCount(string activityId);
        Transition GetForwardTransition(string fromActivityId, string toActivityId);
        IList<Transition> GetForwardTransitionList(string activityId);
        IList<Transition> GetForwardTransitionList(string activityId, IDictionary<string, string> conditionKeyValuePair);
        IList<Transition> GetBackwardTransitionList(string toActivityId);
        Boolean IsValidTransition(Transition transition, IDictionary<string, string> conditionValuePair);
        IList<saiflow.engine.Xpdl.Activity> GetTaskActivityList();
        IList<saiflow.engine.Xpdl.Activity> GetAllTaskActivityList();
        IList<saiflow.engine.Xpdl.Activity> GetAllTaskActivityList(saiflow.engine.Xpdl.Activity splitActivity, saiflow.engine.Xpdl.Activity joinActivity);

        int GetForcedBranchesCountBeforeEOrJoin(saiflow.engine.Xpdl.Activity gatewayActivity, out IList<Transition> forcedTransitionList);

        //Activity type
        Boolean IsMIParallel(saiflow.engine.Xpdl.Activity activity);
        Boolean IsMISequence(saiflow.engine.Xpdl.Activity activity);
        Boolean IsMINode(saiflow.engine.Xpdl.Activity activity);
        Boolean IsMINode(ActivityInstanceEntity activityInstance);
        Boolean IsTaskNode(saiflow.engine.Xpdl.Activity activity);
        Boolean IsTaskNode(ActivityInstanceEntity activityInstance);
        Boolean IsAndSplitMI(saiflow.engine.Xpdl.Activity activity);

        //Resource
        IList<Role> GetRoles();
        IList<Role> GetActivityRoles(string activityId);
        IDictionary<string, PerformerList> GetActivityPerformers(string activityId);
        IDictionary<string, PerformerList> GetActivityPerformers(IList<NodeView> nextActivityTree);
        IList<Form> GetFormList();

        //Notification
        IList<User> GetActivityNotifications(string activityId);
    }
}
