using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
//using System.Diagnostics;
using sailflow.data;
using mooSQL.data;
using sailflow.hub.Form;
using sailflow.hub.Resource;
using sailflow.engine.Utility;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Xpdl
{
    /// <summary>
    /// Process Model Interface
    /// </summary>
    public interface IProcessModel 
    {
        Process Process { get; }
        ProcessEntity ProcessEntity { get; set; }
        sailflow.engine.Xpdl.Activity GetStartActivity();
        sailflow.engine.Xpdl.Activity GetFirstActivity();
        NextActivityMatchedResult GetFirstActivityList(sailflow.engine.Xpdl.Activity startActivity, IDictionary<string, string> conditionKeyValuePair);
        sailflow.engine.Xpdl.Activity GetEndActivity();
        sailflow.engine.Xpdl.Activity GetActivity(string activityId);
        IList<sailflow.engine.Xpdl.Activity> GetActivityList();
        IList<string> GetActivityOutputVarialbeNameList(sailflow.engine.Xpdl.Activity activity);
        sailflow.engine.Xpdl.Activity GetNextActivity(string activityId);
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
        IList<NodeView> GetFirstActivityTreeView(sailflow.engine.Xpdl.Activity startActivity, IDictionary<string, string> conditions);
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
            Expression<Func<ActivityResource, sailflow.engine.Xpdl.Activity, bool>> expression,
            SQLBuilder kit);
        IList<sailflow.engine.Xpdl.Activity> GetNextActivityListWithoutCondition(string activityId);

        IList<NodeView> GetPreviousActivityTree(string currentActivityId);
        IList<sailflow.engine.Xpdl.Activity> GetPreviousActivityList(string currentActivityId);
        IList<sailflow.engine.Xpdl.Activity> GetPreviousActivityList(string currentActivityId, out bool hasGatewayPassed);
        IList<sailflow.engine.Xpdl.Activity> GetFromActivityList(string toActivityId);

        sailflow.engine.Xpdl.Activity GetBackwardGatewayActivity(sailflow.engine.Xpdl.Activity gatewayActivity, 
            ref int joinCount, ref int splitCount);
        Int32 GetBackwardTransitionListCount(string activityId);
        Transition GetForwardTransition(string fromActivityId, string toActivityId);
        IList<Transition> GetForwardTransitionList(string activityId);
        IList<Transition> GetForwardTransitionList(string activityId, IDictionary<string, string> conditionKeyValuePair);
        IList<Transition> GetBackwardTransitionList(string toActivityId);
        Boolean IsValidTransition(Transition transition, IDictionary<string, string> conditionValuePair);
        IList<sailflow.engine.Xpdl.Activity> GetTaskActivityList();
        IList<sailflow.engine.Xpdl.Activity> GetAllTaskActivityList();
        IList<sailflow.engine.Xpdl.Activity> GetAllTaskActivityList(sailflow.engine.Xpdl.Activity splitActivity, sailflow.engine.Xpdl.Activity joinActivity);

        int GetForcedBranchesCountBeforeEOrJoin(sailflow.engine.Xpdl.Activity gatewayActivity, out IList<Transition> forcedTransitionList);

        //Activity type
        Boolean IsMIParallel(sailflow.engine.Xpdl.Activity activity);
        Boolean IsMISequence(sailflow.engine.Xpdl.Activity activity);
        Boolean IsMINode(sailflow.engine.Xpdl.Activity activity);
        Boolean IsMINode(ActivityInstanceEntity activityInstance);
        Boolean IsTaskNode(sailflow.engine.Xpdl.Activity activity);
        Boolean IsTaskNode(ActivityInstanceEntity activityInstance);
        Boolean IsAndSplitMI(sailflow.engine.Xpdl.Activity activity);

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
