using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using sailflow.hub.Form;
using sailflow.hub.Resource;
using sailflow.utils;
using sailflow.events;
using sailflow.data;
using sailflow.result;
using sailflow.xml;

namespace sailflow.service
{
    /// <summary>
    /// Workflow Service Interface
    /// 工作流服务接口
    /// </summary>
    public interface IWorkflowService
    {
        Activity GetFirstActivity(string processId, string version);
        Activity GetActivityEntity(string processId, string version, string actvityId);
        Activity GetNextActivity(string processId, string version, string activityId);
        IList<Activity> GetTaskActivityList(string processId, string version);
        /// <summary>按定义行主键取任务节点列表（非流程设计定义键 ProcessId）。</summary>
        IList<Activity> GetTaskActivityListByProcessDefId(string processDefId);
		IList<Activity> GetAllTaskActivityList(string processId, string version);
        IList<Role> GetActivityRoles(string processId, string version, string activityId);
        NodeView GetNextActivity(string taskId, IDictionary<string, string> condition = null);
        NodeView GetNextActivity(FlowCommandContext context, IDictionary<string, string> condition = null);
        IList<NodeView> GetNextActivityTree(string processId, string version, string activityId, IDictionary<string, string> condition);

        IList<NodeView> GetNextActivityTree(string taskId, IDictionary<string, string> condition = null);
        Task<IList<NodeView>> GetNextActivityTreeAsync(string taskId, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        IList<NodeView> GetNextActivityTree(FlowCommandContext context, IDictionary<string, string> condition = null);
        Task<IList<NodeView>> GetNextActivityTreeAsync(FlowCommandContext context, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 获取下一步活动列表
        /// </summary>
        IList<Activity> GetNextActivityList(string taskId,
            IDictionary<string, string> condition = null);
        
        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 获取下一步活动列表
        /// </summary>
        Task<IList<Activity>> GetNextActivityListAsync(string taskId, 
            IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 根据应用获取流程下一步活动列表
        /// </summary>
        IList<Activity> GetNextActivityList(FlowCommandContext context,
            IDictionary<string, string> condition = null);

        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 根据应用获取流程下一步活动列表
        /// </summary>
        Task<IList<Activity>> GetNextActivityListAsync(FlowCommandContext context,
            IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        IList<NodeView> GetNextActivityRoleUserTree(FlowCommandContext context, IDictionary<string, string> condition = null);
        Task<IList<NodeView>> GetNextActivityRoleUserTreeAsync(FlowCommandContext context, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Business-level API for obtaining the next-step information, including structure, roles and users.
        /// This is the recommended single entry point for UI / application code to query next steps.
        /// 业务层获取“下一步”信息的统一接口，返回结构、角色与用户等完整信息。
        /// 推荐界面层与业务服务统一通过此方法获取下一步，而不要直接依赖 IProcessModel 的结构层方法。
        /// </summary>
        NextStepInfo GetNextStepInfo(FlowCommandContext context, IDictionary<string, string> condition = null);
        Task<NextStepInfo> GetNextStepInfoAsync(FlowCommandContext context, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        IList<NodeView> GetFirstActivityRoleUserTree(FlowCommandContext context, IDictionary<string, string> condition = null);
        Task<IList<NodeView>> GetFirstActivityRoleUserTreeAsync(FlowCommandContext context, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        IList<NodeView> GetPreviousActivityTree(FlowCommandContext context);
        Task<IList<NodeView>> GetPreviousActivityTreeAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        PreviousStepInfo GetPreviousStepInfo(FlowCommandContext context);
        Task<PreviousStepInfo> GetPreviousStepInfoAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        SignForwardStepInfo GetSignForwardStepInfo(FlowCommandContext context);

        void ResetCache(string processId, string version = null);

        [Obsolete("replaced by the new method RunProcess()")]
        WfExecutedResult RunProcessApp(FlowCommandContext context);
    
        //启动流程
        //Startup Process
        WfExecutedResult StartProcess(FlowCommandContext context);
        Task<WfExecutedResult> StartProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        //消息启动流程
        //Message Startup Process
        WfExecutedResult StartProcessByMessage(FlowCommandContext context);
        Task<WfExecutedResult> StartProcessByMessageAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        //运行流程
        //Run Process
        WfExecutedResult RunProcess(FlowCommandContext context);
        Task<WfExecutedResult> RunProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        //自动运行流程
        //Run Automatically Process
        WfExecutedResult RunProcessAuto(FlowCommandContext context);
        Task<WfExecutedResult> RunProcessAutoAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        //撤销流程
        //Withdraw Process
        WfExecutedResult WithdrawProcess(FlowCommandContext context);
        Task<WfExecutedResult> WithdrawProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        //退回流程
        //SendBack Process
        WfExecutedResult SendBackProcess(FlowCommandContext context);
        Task<WfExecutedResult> SendBackProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        //返送流程
        //Resend Process
        WfExecutedResult ResendProcess(FlowCommandContext context);
        Task<WfExecutedResult> ResendProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        //修订流程
        //Revise Process
        WfExecutedResult ReviseProcess(FlowCommandContext context);
        Task<WfExecutedResult> ReviseProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        //驳回流程
        //Reject Process
        WfExecutedResult RejectProcess(FlowCommandContext context);
        Task<WfExecutedResult> RejectProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        //关闭流程
        //Close Process
        WfExecutedResult CloseProcess(FlowCommandContext context);
        Task<WfExecutedResult> CloseProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        //返签流程
        //Reverse Process
        WfExecutedResult ReverseProcess(FlowCommandContext context);
        Task<WfExecutedResult> ReverseProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        //跳转流程
        //Jump Process
        WfExecutedResult JumpProcess(FlowCommandContext context, JumpOptionEnum jumpOption = JumpOptionEnum.Default);
        Task<WfExecutedResult> JumpProcessAsync(FlowCommandContext context, JumpOptionEnum jumpOption = JumpOptionEnum.Default, CancellationToken cancellationToken = default);

        //加签流程
        //Sign Forward Process
        WfExecutedResult SignForwardProcess(FlowCommandContext context);
        Task<WfExecutedResult> SignForwardProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 转办：结束本人待办，在本节点为指定人创建待办，不发生节点流转。
        /// 目标人取自 <c>NextActivityPerformers</c>（任意 ActivityId 键下的 Performer 列表）。
        /// </summary>
        WfExecutedResult TransferProcess(FlowCommandContext context);
        Task<WfExecutedResult> TransferProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 减签：取消指定办理人在本节点的未办待办，不发生节点流转。
        /// 定位活动靠 <c>TaskId</c>；移除对象取自 <c>NextActivityPerformers</c>。至少保留一名打开待办。
        /// </summary>
        WfExecutedResult RemoveAssigneeProcess(FlowCommandContext context);
        Task<WfExecutedResult> RemoveAssigneeProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 认领/承办：仅 Leave=Race/GroupRace；旁关他人（或同组）打开待办、只留自己，不发生节点流转。
        /// 定位靠 <c>TaskId</c>。
        /// </summary>
        WfExecutedResult TakeOnProcess(FlowCommandContext context);
        Task<WfExecutedResult> TakeOnProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 改派：用 <c>NextActivityPerformers</c> 作为当前节点新办理人全集（差量关旧/建新），不发生节点流转。
        /// 定位活动靠 <c>TaskId</c>。
        /// </summary>
        WfExecutedResult ChangeAssigneeProcess(FlowCommandContext context);
        Task<WfExecutedResult> ChangeAssigneeProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 加人：向当前扁节点追加普通办理人待办（增量）；不改 Leave、不压加签栈、不发生节点流转。
        /// 定位活动靠 <c>TaskId</c>；新人取自 <c>NextActivityPerformers</c>。
        /// </summary>
        WfExecutedResult AddAssigneeProcess(FlowCommandContext context);
        Task<WfExecutedResult> AddAssigneeProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 催办：对当前节点未办待办发催办通知（不改状态、不流转）。
        /// <c>TaskId</c> 定位活动；可选 <c>NextActivityPerformers</c> 限定对象（空=全部打开待办）。
        /// </summary>
        WfExecutedResult UrgeProcess(FlowCommandContext context);
        Task<WfExecutedResult> UrgeProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 委派：本人待办交一人代办；受托人办结后自动回转原责任人（不推进）。≠ Transfer。
        /// <c>TaskId</c> + <c>NextActivityPerformers</c>（取首个有效用户）。
        /// </summary>
        WfExecutedResult DelegateProcess(FlowCommandContext context);
        Task<WfExecutedResult> DelegateProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// 沟通：为选中人建参与者待办（阅办）；锁定发起人审批直至全员阅办或收回。
        /// <c>TaskId</c> + <c>NextActivityPerformers</c>。
        /// </summary>
        WfExecutedResult ConsultProcess(FlowCommandContext context);
        Task<WfExecutedResult> ConsultProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        /// <summary>阅办：参与者办结自己的沟通待办。<c>TaskId</c>。</summary>
        WfExecutedResult AcknowledgeProcess(FlowCommandContext context);
        Task<WfExecutedResult> AcknowledgeProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        /// <summary>收回沟通：取消源 Task 下未关闭的参与者待办。<c>TaskId</c>=发起人审批待办。</summary>
        WfExecutedResult RecallConsultProcess(FlowCommandContext context);
        Task<WfExecutedResult> RecallConsultProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        //审批决策
        //Approval
        void AgreeTask(string taskId);
        void RefuseTask(string taskId);

        /// <summary>挂起：<c>context.TaskId</c> 定位实例；走 Lifecycle 动作骨架（Gate/Hook）。</summary>
        WfExecutedResult SuspendProcess(FlowCommandContext context);
        Task<WfExecutedResult> SuspendProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);
        /// <summary>恢复：<c>context.ProcessInstanceId</c> 定位实例。</summary>
        WfExecutedResult ResumeProcess(FlowCommandContext context);
        Task<WfExecutedResult> ResumeProcessAsync(FlowCommandContext context, CancellationToken cancellationToken = default);

        /// <summary>兼容：按 taskId 挂起，成功返回 true。</summary>
        Boolean SuspendProcess(string taskId, FlowCommandContext context);
        /// <summary>兼容：按 processInstanceId 恢复，成功返回 true。（参数名历史为 activityInstanceId，实为流程实例 Id）</summary>
        Boolean ResumeProcess(string processInstanceId, FlowCommandContext context);

        /// <summary>取消：BizPK+ProcessId(+Version)；Lifecycle 族。</summary>
        WfExecutedResult CancelProcess(FlowCommandContext canceler);
        Task<WfExecutedResult> CancelProcessAsync(FlowCommandContext canceler, CancellationToken cancellationToken = default);
        /// <summary>作废：BizPK+ProcessId(+Version)；Lifecycle 族。</summary>
        WfExecutedResult DiscardProcess(FlowCommandContext discarder);
        Task<WfExecutedResult> DiscardProcessAsync(FlowCommandContext discarder, CancellationToken cancellationToken = default);
        /// <summary>终止：ProcessInstanceId 或 BizPK+ProcessId；Lifecycle 族。</summary>
        WfExecutedResult TerminateProcess(FlowCommandContext terminator);
        Task<WfExecutedResult> TerminateProcessAsync(FlowCommandContext terminator, CancellationToken cancellationToken = default);
        Boolean SetTaskRead(FlowCommandContext context);
        WfDataManagedResult EntrustTask(TaskEntrustedEntity entrusted, bool cancalOriginalTask = true);
        void CancelEntrustedTask(string id);
        Boolean ReplaceTask(string taskId, List<TaskReplacedEntity> replaced, FlowCommandContext context);
        Boolean SetProcessOverdue(string processInstanceId, DateTime overdueDateTime, FlowCommandContext context);
        void SetActivityJobTimerCompleted(string activityInstanceId);
        void SetProcessJobTimerCompleted(string processInstanceId);
        string SaveProcessVariable(ProcessVariableEntity entity);
        string SaveProcessVariable(mooSQL.data.SQLBuilder kit, ProcessVariableEntity entity);

        User GetProcessInitiator(string processInstanceId);
        User GetProcessInitiator(mooSQL.data.SQLBuilder kit, string processInstanceId);

        TaskViewEntity GetTaskView(string taskId);
        TaskViewEntity GetTaskView(string processInstanceId, string activityInstanceId);
        TaskViewEntity GetTaskView(string bizPK, string processId, string userId);
        [Obsolete("replaced by the GetRunningTask, mainly used to run forward method internal")]
        ActivityInstanceEntity GetRunningNode(FlowCommandContext context);

        ProcessInstanceEntity GetRunningProcessInstance(FlowCommandContext context);
        ProcessInstanceEntity GetProcessInstance(string processInstanceId);
        IList<ProcessInstanceEntity> GetProcessInstanceListByBizPK(string bizPK);
        ProcessInstanceEntity GetProcessInstanceByActivity(string activityInstanceId);
        void UpdateProcessInstance(ProcessInstanceEntity entity);

        IList<ActivityInstanceEntity> GetActivityInstanceList(string processInstanceId);
        IList<ActivityInstanceEntity> GetTargetActivityInstanceList(string fromActivityInstanceId);
        ActivityInstanceEntity GetActivityInstance(string activityInstanceId);
        ProcessEntity GetProcessById(string id);
        ProcessEntity GetProcessUsing(string processId);
        ProcessEntity GetProcessByVersion(string processId, string version);
        ProcessEntity GetProcessByName(string processName, string version = null);
        ProcessEntity GetProcessByCode(string processCode, string version = null);
        IList<ProcessEntity> GetProcessList();

        ProcessFileEntity GetProcessFile(string processId, string version);
        ProcessFileEntity GetProcessFile(mooSQL.data.SQLBuilder kit, string processId, string version);
        ProcessFileEntity GetProcessFileById(string id);
        void SaveProcessFile(ProcessFileEntity entity);

        string CreateProcess(ProcessEntity entity);
        string CreateProcessVersion(ProcessEntity entity);
        ProcessFileEntity CreateProcessByXML(string xmlContent);
        string InsertProcess(ProcessEntity entity);
        void UpdateProcess(ProcessEntity entity);
        void UpdateProcessUsingState(string processId, string version, byte usingState);
        void UpgradeProcess(string processId, string version, string newVersion);
        void DeleteProcess(string processId, string version);
		void DeleteProcess(string id);
        /// <summary>按流程设计定义键 + 版本删除该定义下全部运转实例及关联数据。</summary>
        bool DeleteProcessInstances(string processId, string version);
        void ImportProcess(string xmlContent);
        ProcessValidateResult ValidateProcess(ProcessEntity processValidateEntity);
        

        //角色资源接口
        //Role Resource Interfaces
        IList<Role> GetRoleAll();
        IList<Role> GetRoleByProcess(string processId, string version);
        IList<Role> GetRoleUserListByProcess(string processId, string version);
        IList<User> GetUserAll();
        IList<User> GetUserListByRole(string roleId);
        PerformerList GetPerformerList(NodeView nextNode);

        //表单资源接口
        //Form Resource Interfaces
        IList<Form> GetFormAll();
        IList<Form> GetFormList(string processId, string version);
        IList<Form> GetFormFieldEditComp(FieldQuery query);

        //流程变量接口
        //Process Variables Interfaces
        IList<ProcessVariableEntity> GetProcessVariableList(ProcessVariableQuery query);
        IList<ProcessVariableEntity> GetProcessVariableList(mooSQL.data.SQLBuilder kit, ProcessVariableQuery query);
        ProcessVariableEntity GetProcessVariable(ProcessVariableQuery query);
        ProcessVariableEntity GetProcessVariable(mooSQL.data.SQLBuilder kit, ProcessVariableQuery query);
        ProcessVariableEntity GetProcessVariable(string processId, string variableId);
        Boolean ValidateProcessVariable(string processInstanceId, string expression);
        void DeleteProcessVariable(string processId, string variableId);
        
        //流程变量定义接口（sf_variable）
        IList<VariableEntity> GetVariableDefinitionList(string processId, string version, string activityId);
        VariableEntity GetVariableDefinition(string id);
        string SaveVariableDefinition(VariableEntity entity);
        void DeleteVariableDefinition(string id);
        void SaveVariableDefinitionList(string processId, string version, string activityId, IList<VariableEntity> list);

        //规则集接口（sf_rule_set）
        IList<RuleSetEntity> GetRuleSetList();
        RuleSetEntity GetRuleSet(string ruleSetCode);
        string SaveRuleSet(RuleSetEntity entity);
        void DeleteRuleSet(string ruleSetCode);

        // 链式建造见 SailClient.CreateRunBuilder（B）；本接口仅为无状态执行（吃 FlowCommandContext）。
    }
}
