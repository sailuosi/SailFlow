using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.data;
using sailflow.hub.Localize;
using sailflow.hub.Resource;
using sailflow.assignee;
using sailflow.utils;
using sailflow.hooks;
using sailflow.xml;
using sailflow.result;
using sailflow.hosting;
using sailflow.log;
using sailflow.run;
using sailflow.factory;

namespace sailflow.data;

/// <summary>契约 · ActivityInstanceManager（实现位于 sailflow.runtime）。</summary>
public interface IActivityInstanceManager
{
    /// <summary>是否为加签席位容器（发起人 AI，承载子规则 Leave）。</summary>
    static bool IsSignForwardContainer(ActivityInstanceEntity ai)
        => ai != null
           && ai.ComplexType == (short)ComplexTypeEnum.SignForward
           && !string.IsNullOrEmpty(ai.LeaveJudgeType);

    ActivityInstanceEntity GetById(string activityInstanceId);

    ActivityInstanceEntity GetById(SQLBuilder kit, string activityInstanceId);

    Task<ActivityInstanceEntity> GetByIdAsync( SQLBuilder kit, string activityInstanceId, CancellationToken cancellationToken = default);

    ActivityInstanceEntity GetRunningNode(FlowCommandContext runner);

    ActivityInstanceEntity GetRunningNode(FlowCommandContext runner, out TaskViewEntity taskView);

    ActivityInstanceEntity GetRunningNode(SQLBuilder kit, FlowCommandContext runner, out TaskViewEntity taskView);

    Task<(ActivityInstanceEntity Node, TaskViewEntity TaskView)> GetRunningNodeAsync( SQLBuilder kit, FlowCommandContext runner, CancellationToken cancellationToken = default);

    ActivityInstanceEntity GetBackSrcActivityInstance(string currentActivityInstanceId);

    bool IsMineTask(ActivityInstanceEntity entity, string userId);

    Boolean IncludeRunningState(ActivityInstanceEntity activityInstance);

    IList<ActivityInstanceEntity> GetActivityInstanceList(SQLBuilder kit, string processInstanceId);

    ActivityInstanceEntity GetActivityInstanceLatest(string processInstanceId, string activityId);

    ActivityInstanceEntity GetActivityInstanceLatest(SQLBuilder kit, string processInstanceId, string activityId);

    ActivityInstanceEntity GetActivityInstanceLatest(string bizPK, string processId, string activityId);

    ActivityInstanceEntity GetActivityInstanceLatest(SQLBuilder kit, string bizPK, string processId, string activityId);

    List<ActivityInstanceEntity> GetActivityInstanceList(string processId, string version, string activityId);

    ActivityInstanceEntity GetActivityRunning(SQLBuilder kit, string processInstanceId, string activityId);

    ActivityInstanceEntity GetActivityByState(SQLBuilder kit, string processInstanceId, string activityId, ActivityStateEnum activityState);

    List<ActivityInstanceEntity> GetActivityInstanceListCompleted(string bizPK, string processId);

    ActivityInstanceEntity GetPreviousActivityInstanceSimple(ActivityInstanceEntity runningNode, string previousActivityId);

    ActivityInstanceEntity GetPreviousActivityInstanceSimple(SQLBuilder kit, ActivityInstanceEntity runningNode, string previousActivityId);

    IList<ActivityInstanceEntity> GetActivityInstanceListCompletedSimple(string processInstanceId, string activityId);

    IList<ActivityInstanceEntity> GetActivityInstanceListCompletedSimple(SQLBuilder kit, string processInstanceId, string activityId);

    ActivityInstanceEntity GetByTask(string taskId);

    ActivityInstanceEntity GetByTask(SQLBuilder kit, string taskId);

    IEnumerable<ActivityInstanceEntity> GetRunningActivityInstanceList(string bizPK, string processId, string version = null);

    IEnumerable<ActivityInstanceEntity> GetRunningActivityInstanceList(SQLBuilder kit, string bizPK, string processId, string version);

    Task<IEnumerable<ActivityInstanceEntity>> GetRunningActivityInstanceListAsync( SQLBuilder kit, string bizPK, string processId, string version, CancellationToken cancellationToken = default);

    List<ActivityInstanceEntity> GetMultipleInstanceListCompleted(string mainActivityInstanceId);

    List<ActivityInstanceEntity> GetMultipleInstanceListCompleted(SQLBuilder kit, string mainActivityInstanceId);

    List<ActivityInstanceEntity> GetPreviousParallelMultipleInstanceListCompleted(ActivityInstanceEntity runningNode, string previousActivityId);

    List<ActivityInstanceEntity> GetPreviousParallelMultipleInstanceListCompleted(SQLBuilder kit, ActivityInstanceEntity runningNode, string previousActivityId);

    Boolean IsMultipleInstanceChildren(ActivityInstanceEntity child);

    List<ActivityInstanceEntity> GetActivityMulitipleInstanceWithState(string mainActivityInstanceId, string processInstanceId, short? activityState = null);

    IList<ActivityInstanceEntity> GetActivityMulitipleInstanceWithStateBatch(SQLBuilder kit, string processInstanceId, string mainActivityInstanceId, short? activityState);

    List<ActivityInstanceEntity> GetActivityMulitipleInstanceWithState(SQLBuilder kit, string mainActivityInstanceId, string processInstanceId, short? activityState);

    int GetGatewayInstanceCountByTransition(SQLBuilder kit, string splitActivityId, string splitActivityInstanceId, string processInstanceId);

    List<ActivityInstanceEntity> GetValidSplitedActivityInstanceList(SQLBuilder kit, string processInstanceId, string splitGatewayInstanceId);

    List<ActivityInstanceEntity> GetValidActivityInstanceListOfMI(string mainActivityInstanceId, string processInstanceId);

    List<ActivityInstanceEntity> GetValidActivityInstanceListOfMI(SQLBuilder kit, string mainActivityInstanceId, string processInstanceId);

    ActivityInstanceEntity CreateActivityInstanceObject(string bizChannel, string bizPK, string bizCode, string processId, string processInstanceId, Activity activity, FlowCommandContext runner);

    ActivityInstanceEntity CreateActivityInstanceObject(ActivityInstanceEntity main);

    ActivityInstanceEntity CreateBackwardActivityInstanceObject(string bizChannel, string bizPK, string bizCode, string processInstanceId, string processId, Activity activity, BackwardTypeEnum backwardType, string backSrcActivityInstanceId, string backOrgActivityInstanceId, FlowCommandContext runner);

    void IncreaseTokensHad(SQLBuilder kit, string activityInstanceId, FlowCommandContext runner);

    void Read(SQLBuilder kit, string activityInstanceId, FlowCommandContext runner);

    void Withdraw(SQLBuilder kit, string activityInstanceId, FlowCommandContext runner);

    void SendBack(SQLBuilder kit, string activityInstanceId, FlowCommandContext runner);

    void Complete(SQLBuilder kit, string activityInstanceId, FlowCommandContext runner);

    void Complete(SQLBuilder kit, ActivityInstanceEntity activityInstance, FlowCommandContext runner);

    void SetApprovalStatus(SQLBuilder kit, string activityInstanceId);

    void UpdateActivityInstanceBlockedBetweenSplitJoin(SQLBuilder kit, Activity gatewayActivity, ActivityInstanceEntity gatewayInstance, IProcessModel processModel);

    string Insert(ActivityInstanceEntity entity);

    string Insert(SQLBuilder kit, ActivityInstanceEntity entity);

    void Update(ActivityInstanceEntity entity);

    void Update(SQLBuilder kit, ActivityInstanceEntity entity);

    void Cancel(SQLBuilder kit, string activityInstanceId, FlowCommandContext runner);

    void CancelUnCompletedMultipleInstance(SQLBuilder kit, string mainActivityInstanceId, FlowCommandContext runner);

    void CancelSignForwardSubtreeUnder(SQLBuilder kit, string rootActivityInstanceId, FlowCommandContext runner);

    void WithdrawMainIncludedChildNodes(SQLBuilder kit, string mainActivityInstanceId, FlowCommandContext runner);

    void Resuspend(SQLBuilder kit, string activityInstanceId, FlowCommandContext runner);

    void Rerun(SQLBuilder kit, string activityInstanceId, FlowCommandContext runner);

    void Delete(string activityInstanceId);

    void Delete(SQLBuilder kit, string activityInstanceId);

    void Agree(string taskId);

    void Agree(SQLBuilder kit, string taskId);

    void Refuse(string taskId);

    void Refuse(SQLBuilder kit, string taskId);

    NodePassedResult CheckActivityInstancePassedResult(SQLBuilder kit, string activityInstanceId);

    NodePassedTypeEnum CheckMIPassRateInfo(SQLBuilder kit, ActivityInstanceEntity currentActivityInstance, ActivityInstanceEntity mainActivityInstance);

    NodePassedTypeEnum EvaluateLeaveOnActivity( SQLBuilder kit, ActivityInstanceEntity currentActivityInstance, ActivityInstanceEntity subjectActivityInstance);

    Boolean GetMiApprovalThresholdStatus(SQLBuilder kit, ActivityInstanceEntity currentActivityInstance, ActivityInstanceEntity mainActivityInstance);
}
