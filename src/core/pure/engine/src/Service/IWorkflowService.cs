using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using sailflow.hub.Form;
using sailflow.hub.Resource;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.Entity;
using sailflow.engine.Result;
using sailflow.engine.Xpdl;
using sailflow.engine.Result;

namespace sailflow.engine.Service
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
        NodeView GetNextActivity(FlowCommandContext runner, IDictionary<string, string> condition = null);
        IList<NodeView> GetNextActivityTree(string processId, string version, string activityId, IDictionary<string, string> condition);

        IList<NodeView> GetNextActivityTree(string taskId, IDictionary<string, string> condition = null);
        Task<IList<NodeView>> GetNextActivityTreeAsync(string taskId, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        IList<NodeView> GetNextActivityTree(FlowCommandContext runner, IDictionary<string, string> condition = null);
        Task<IList<NodeView>> GetNextActivityTreeAsync(FlowCommandContext runner, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

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
        IList<Activity> GetNextActivityList(FlowCommandContext runner,
            IDictionary<string, string> condition = null);

        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 根据应用获取流程下一步活动列表
        /// </summary>
        Task<IList<Activity>> GetNextActivityListAsync(FlowCommandContext runner,
            IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        IList<NodeView> GetNextActivityRoleUserTree(FlowCommandContext runner, IDictionary<string, string> condition = null);
        Task<IList<NodeView>> GetNextActivityRoleUserTreeAsync(FlowCommandContext runner, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Business-level API for obtaining the next-step information, including structure, roles and users.
        /// This is the recommended single entry point for UI / application code to query next steps.
        /// 业务层获取“下一步”信息的统一接口，返回结构、角色与用户等完整信息。
        /// 推荐界面层与业务服务统一通过此方法获取下一步，而不要直接依赖 IProcessModel 的结构层方法。
        /// </summary>
        NextStepInfo GetNextStepInfo(FlowCommandContext runner, IDictionary<string, string> condition = null);
        Task<NextStepInfo> GetNextStepInfoAsync(FlowCommandContext runner, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        IList<NodeView> GetFirstActivityRoleUserTree(FlowCommandContext runner, IDictionary<string, string> condition = null);
        Task<IList<NodeView>> GetFirstActivityRoleUserTreeAsync(FlowCommandContext runner, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        IList<NodeView> GetPreviousActivityTree(FlowCommandContext runner);
        Task<IList<NodeView>> GetPreviousActivityTreeAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        PreviousStepInfo GetPreviousStepInfo(FlowCommandContext runner);
        Task<PreviousStepInfo> GetPreviousStepInfoAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        SignForwardStepInfo GetSignForwardStepInfo(FlowCommandContext runner);

        void ResetCache(string processId, string version = null);

        [Obsolete("replaced by the new method RunProcess()")]
        WfExecutedResult RunProcessApp(FlowCommandContext runner);
    
        //启动流程
        //Startup Process
        WfExecutedResult StartProcess(FlowCommandContext runner);
        Task<WfExecutedResult> StartProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        //消息启动流程
        //Message Startup Process
        WfExecutedResult StartProcessByMessage(FlowCommandContext runner);
        Task<WfExecutedResult> StartProcessByMessageAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        //运行流程
        //Run Process
        WfExecutedResult RunProcess(FlowCommandContext runner);
        Task<WfExecutedResult> RunProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        //自动运行流程
        //Run Automatically Process
        WfExecutedResult RunProcessAuto(FlowCommandContext runner);
        Task<WfExecutedResult> RunProcessAutoAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        //撤销流程
        //Withdraw Process
        WfExecutedResult WithdrawProcess(FlowCommandContext runner);
        Task<WfExecutedResult> WithdrawProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        //退回流程
        //SendBack Process
        WfExecutedResult SendBackProcess(FlowCommandContext runner);
        Task<WfExecutedResult> SendBackProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        //返送流程
        //Resend Process
        WfExecutedResult ResendProcess(FlowCommandContext runner);
        Task<WfExecutedResult> ResendProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        //修订流程
        //Revise Process
        WfExecutedResult ReviseProcess(FlowCommandContext runner);
        Task<WfExecutedResult> ReviseProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        //驳回流程
        //Reject Process
        WfExecutedResult RejectProcess(FlowCommandContext runner);
        Task<WfExecutedResult> RejectProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        //关闭流程
        //Close Process
        WfExecutedResult CloseProcess(FlowCommandContext runner);
        Task<WfExecutedResult> CloseProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        //返签流程
        //Reverse Process
        WfExecutedResult ReverseProcess(FlowCommandContext runner);
        Task<WfExecutedResult> ReverseProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        //跳转流程
        //Jump Process
        WfExecutedResult JumpProcess(FlowCommandContext runner, JumpOptionEnum jumpOption = JumpOptionEnum.Default);
        Task<WfExecutedResult> JumpProcessAsync(FlowCommandContext runner, JumpOptionEnum jumpOption = JumpOptionEnum.Default, CancellationToken cancellationToken = default);

        //加签流程
        //Sign Forward Process
        WfExecutedResult SignForwardProcess(FlowCommandContext runner);
        Task<WfExecutedResult> SignForwardProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        /// <summary>
        /// 转办：结束本人待办，在本节点为指定人创建待办，不发生节点流转。
        /// 目标人取自 <c>NextActivityPerformers</c>（任意 ActivityId 键下的 Performer 列表）。
        /// </summary>
        WfExecutedResult TransferProcess(FlowCommandContext runner);
        Task<WfExecutedResult> TransferProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default);

        //审批决策
        //Approval
        void AgreeTask(string taskId);
        void RefuseTask(string taskId);

        Boolean ResumeProcess(string activityInstanceId, FlowCommandContext runner);
        Boolean SuspendProcess(string taskId, FlowCommandContext runner);
        Boolean CancelProcess(FlowCommandContext canceler);
        Boolean DiscardProcess(FlowCommandContext discarder);
        Boolean TerminateProcess(FlowCommandContext terminator);
        Boolean SetTaskRead(FlowCommandContext runner);
        WfDataManagedResult EntrustTask(TaskEntrustedEntity entrusted, bool cancalOriginalTask = true);
        void CancelEntrustedTask(string id);
        Boolean ReplaceTask(string taskId, List<TaskReplacedEntity> replaced, FlowCommandContext runner);
        Boolean SetProcessOverdue(string processInstanceId, DateTime overdueDateTime, FlowCommandContext runner);
        void SetActivityJobTimerCompleted(string activityInstanceId);
        void SetProcessJobTimerCompleted(string processInstanceId);
        string SaveProcessVariable(ProcessVariableEntity entity);

        User GetProcessInitiator(string processInstanceId);

        TaskViewEntity GetTaskView(string taskId);
        TaskViewEntity GetTaskView(string processInstanceId, string activityInstanceId);
        TaskViewEntity GetTaskView(string bizPK, string processId, string userId);
        [Obsolete("replaced by the GetRunningTask, mainly used to run forward method internal")]
        ActivityInstanceEntity GetRunningNode(FlowCommandContext runner);

        ProcessInstanceEntity GetRunningProcessInstance(FlowCommandContext runner);
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
        ProcessVariableEntity GetProcessVariable(ProcessVariableQuery query);
        ProcessVariableEntity GetProcessVariable(string processId, string variableId);
        Boolean ValidateProcessVariable(string processInstanceId, string expression);
        void DeleteProcessVariable(string processId, string variableId);
        
        //流程变量定义接口（wf_variable）
        IList<VariableEntity> GetVariableDefinitionList(string processId, string version, string activityId);
        VariableEntity GetVariableDefinition(string id);
        string SaveVariableDefinition(VariableEntity entity);
        void DeleteVariableDefinition(string id);
        void SaveVariableDefinitionList(string processId, string version, string activityId, IList<VariableEntity> list);

        //规则集接口（wf_rule_set）
        IList<RuleSetEntity> GetRuleSetList();
        RuleSetEntity GetRuleSet(string ruleSetCode);
        string SaveRuleSet(RuleSetEntity entity);
        void DeleteRuleSet(string ruleSetCode);

        // 链式建造见 SailClient.CreateRunBuilder（B）；本接口仅为无状态执行（吃 FlowCommandContext）。
    }
}
