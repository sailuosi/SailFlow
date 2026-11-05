using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using saiflow.hub.Form;
using saiflow.hub.Resource;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Entity;
using saiflow.engine.Result;
using saiflow.engine.Xpdl;
using saiflow.engine.Result;

namespace saiflow.engine.Service
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
        IList<Activity> GetTaskActivityList(int processId);
		IList<Activity> GetAllTaskActivityList(string processId, string version);
        IList<Role> GetActivityRoles(string processId, string version, string activityId);
        NodeView GetNextActivity(int taskId, IDictionary<string, string> condition = null);
        NodeView GetNextActivity(WfAppRunner runner, IDictionary<string, string> condition = null);
        IList<NodeView> GetNextActivityTree(string processId, string version, string activityId, IDictionary<string, string> condition);

        IList<NodeView> GetNextActivityTree(int taskId, IDictionary<string, string> condition = null);
        Task<IList<NodeView>> GetNextActivityTreeAsync(int taskId, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        IList<NodeView> GetNextActivityTree(WfAppRunner runner, IDictionary<string, string> condition = null);
        Task<IList<NodeView>> GetNextActivityTreeAsync(WfAppRunner runner, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 获取下一步活动列表
        /// </summary>
        IList<Activity> GetNextActivityList(int taskId,
            IDictionary<string, string> condition = null);
        
        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 获取下一步活动列表
        /// </summary>
        Task<IList<Activity>> GetNextActivityListAsync(int taskId, 
            IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 根据应用获取流程下一步活动列表
        /// </summary>
        IList<Activity> GetNextActivityList(WfAppRunner runner,
            IDictionary<string, string> condition = null);

        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 根据应用获取流程下一步活动列表
        /// </summary>
        Task<IList<Activity>> GetNextActivityListAsync(WfAppRunner runner,
            IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        IList<NodeView> GetNextActivityRoleUserTree(WfAppRunner runner, IDictionary<string, string> condition = null);
        Task<IList<NodeView>> GetNextActivityRoleUserTreeAsync(WfAppRunner runner, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Business-level API for obtaining the next-step information, including structure, roles and users.
        /// This is the recommended single entry point for UI / application code to query next steps.
        /// 业务层获取“下一步”信息的统一接口，返回结构、角色与用户等完整信息。
        /// 推荐界面层与业务服务统一通过此方法获取下一步，而不要直接依赖 IProcessModel 的结构层方法。
        /// </summary>
        NextStepInfo GetNextStepInfo(WfAppRunner runner, IDictionary<string, string> condition = null);
        Task<NextStepInfo> GetNextStepInfoAsync(WfAppRunner runner, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        IList<NodeView> GetFirstActivityRoleUserTree(WfAppRunner runner, IDictionary<string, string> condition = null);
        Task<IList<NodeView>> GetFirstActivityRoleUserTreeAsync(WfAppRunner runner, IDictionary<string, string> condition = null, CancellationToken cancellationToken = default);

        IList<NodeView> GetPreviousActivityTree(WfAppRunner runner);
        Task<IList<NodeView>> GetPreviousActivityTreeAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        PreviousStepInfo GetPreviousStepInfo(WfAppRunner runner);
        Task<PreviousStepInfo> GetPreviousStepInfoAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        SignForwardStepInfo GetSignForwardStepInfo(WfAppRunner runner);

        void ResetCache(string processId, string version = null);

        [Obsolete("replaced by the new method RunProcess()")]
        WfExecutedResult RunProcessApp(WfAppRunner runner);
    
        //启动流程
        //Startup Process
        WfExecutedResult StartProcess(WfAppRunner runner);
        Task<WfExecutedResult> StartProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        //消息启动流程
        //Message Startup Process
        WfExecutedResult StartProcessByMessage(WfAppRunner runner);
        Task<WfExecutedResult> StartProcessByMessageAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        //运行流程
        //Run Process
        WfExecutedResult RunProcess(WfAppRunner runner);
        Task<WfExecutedResult> RunProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        //自动运行流程
        //Run Automatically Process
        WfExecutedResult RunProcessAuto(WfAppRunner runner);
        Task<WfExecutedResult> RunProcessAutoAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        //撤销流程
        //Withdraw Process
        WfExecutedResult WithdrawProcess(WfAppRunner runner);
        Task<WfExecutedResult> WithdrawProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        //退回流程
        //SendBack Process
        WfExecutedResult SendBackProcess(WfAppRunner runner);
        Task<WfExecutedResult> SendBackProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        //返送流程
        //Resend Process
        WfExecutedResult ResendProcess(WfAppRunner runner);
        Task<WfExecutedResult> ResendProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        //修订流程
        //Revise Process
        WfExecutedResult ReviseProcess(WfAppRunner runner);
        Task<WfExecutedResult> ReviseProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        //驳回流程
        //Reject Process
        WfExecutedResult RejectProcess(WfAppRunner runner);
        Task<WfExecutedResult> RejectProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        //关闭流程
        //Close Process
        WfExecutedResult CloseProcess(WfAppRunner runner);
        Task<WfExecutedResult> CloseProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        //返签流程
        //Reverse Process
        WfExecutedResult ReverseProcess(WfAppRunner runner);
        Task<WfExecutedResult> ReverseProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        //跳转流程
        //Jump Process
        WfExecutedResult JumpProcess(WfAppRunner runner, JumpOptionEnum jumpOption = JumpOptionEnum.Default);
        Task<WfExecutedResult> JumpProcessAsync(WfAppRunner runner, JumpOptionEnum jumpOption = JumpOptionEnum.Default, CancellationToken cancellationToken = default);

        //加签流程
        //Sign Forward Process
        WfExecutedResult SignForwardProcess(WfAppRunner runner);
        Task<WfExecutedResult> SignForwardProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        /// <summary>
        /// 转办：结束本人待办，在本节点为指定人创建待办，不发生节点流转。
        /// 目标人取自 <c>NextActivityPerformers</c>（任意 ActivityId 键下的 Performer 列表）。
        /// </summary>
        WfExecutedResult TransferProcess(WfAppRunner runner);
        Task<WfExecutedResult> TransferProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default);

        //审批决策
        //Approval
        void AgreeTask(int taskId);
        void RefuseTask(int taskId);

        Boolean ResumeProcess(int activityInstanceId, WfAppRunner runner);
        Boolean SuspendProcess(int taskId, WfAppRunner runner);
        Boolean CancelProcess(WfAppRunner canceler);
        Boolean DiscardProcess(WfAppRunner discarder);
        Boolean TerminateProcess(WfAppRunner terminator);
        Boolean SetTaskRead(WfAppRunner runner);
        WfDataManagedResult EntrustTask(TaskEntrustedEntity entrusted, bool cancalOriginalTask = true);
        void CancelEntrustedTask(int id);
        Boolean ReplaceTask(int taskId, List<TaskReplacedEntity> replaced, WfAppRunner runner);
        Boolean SetProcessOverdue(int processInstanceId, DateTime overdueDateTime, WfAppRunner runner);
        void SetActivityJobTimerCompleted(int activityInstanceId);
        void SetProcessJobTimerCompleted(int processInstanceId);
        int SaveProcessVariable(ProcessVariableEntity entity);

        User GetProcessInitiator(int processInstanceId);

        TaskViewEntity GetTaskView(int taskId);
        TaskViewEntity GetTaskView(int processInstanceId, int activityInstanceId);
        TaskViewEntity GetTaskView(string appInstanceId, string processId, string userId);
        [Obsolete("replaced by the GetRunningTask, mainly used to run forward method internal")]
        ActivityInstanceEntity GetRunningNode(WfAppRunner runner);

        ProcessInstanceEntity GetRunningProcessInstance(WfAppRunner runner);
        ProcessInstanceEntity GetProcessInstance(int processInstanceId);
        IList<ProcessInstanceEntity> GetProcessInstance(string appInstanceId);
        ProcessInstanceEntity GetProcessInstanceByActivity(int activityInstanceId);
        void UpdateProcessInstance(ProcessInstanceEntity entity);

        IList<ActivityInstanceEntity> GetActivityInstanceList(int processInstanceId);
        IList<ActivityInstanceEntity> GetTargetActivityInstanceList(int fromActivityInstanceId);
        ActivityInstanceEntity GetActivityInstance(int activityInstanceId);
        ProcessEntity GetProcessById(int id);
        ProcessEntity GetProcessUsing(string processId);
        ProcessEntity GetProcessByVersion(string processId, string version);
        ProcessEntity GetProcessByName(string processName, string version = null);
        ProcessEntity GetProcessByCode(string processCode, string version = null);
        IList<ProcessEntity> GetProcessList();

        ProcessFileEntity GetProcessFile(string processId, string version);
        ProcessFileEntity GetProcessFileById(int id);
        void SaveProcessFile(ProcessFileEntity entity);

        int CreateProcess(ProcessEntity entity);
        int CreateProcessVersion(ProcessEntity entity);
        ProcessFileEntity CreateProcessByXML(string xmlContent);
        int InsertProcess(ProcessEntity entity);
        void UpdateProcess(ProcessEntity entity);
        void UpdateProcessUsingState(string processId, string version, byte usingState);
        void UpgradeProcess(string processId, string version, string newVersion);
        void DeleteProcess(string processId, string version);
		void DeleteProcess(int id);
        bool DeleteInstanceInt(string processId, string version);
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
        ProcessVariableEntity GetProcessVariable(int variableId);
        Boolean ValidateProcessVariable(int processInstanceId, string expression);
        void DeleteProcessVariable(int variableId);
        
        //流程变量定义接口（wf_variable）
        IList<VariableEntity> GetVariableDefinitionList(string processId, string version, string activityId);
        VariableEntity GetVariableDefinition(int id);
        int SaveVariableDefinition(VariableEntity entity);
        void DeleteVariableDefinition(int id);
        void SaveVariableDefinitionList(string processId, string version, string activityId, IList<VariableEntity> list);

        //规则集接口（wf_rule_set）
        IList<RuleSetEntity> GetRuleSetList();
        RuleSetEntity GetRuleSet(string ruleSetCode);
        int SaveRuleSet(RuleSetEntity entity);
        void DeleteRuleSet(string ruleSetCode);

        #region Chain Service Interface
        IWorkflowService CreateRunner(WfAppRunner runner);
        IWorkflowService CreateRunner(string userId, string userName);
        IWorkflowService UseApp(string appInstanceId, string appName, string appCode = null);
        IWorkflowService UseProcess(string processId, string version = null);
        IWorkflowService IfCondition(IDictionary<string, string> conditions);
        IWorkflowService IfCondition(string name, string value);
        IWorkflowService OnTask(int taskId);
        IWorkflowService SetVariable(string name, string value);
        IWorkflowService SetVariable(IDictionary<string, string> variables);
        IWorkflowService NextStep(string activityId, PerformerList performerList);
        IWorkflowService NextStep(IDictionary<string, PerformerList> nextActivityPerformers);
        IWorkflowService NextStepInt(string userId, string userName);
        IWorkflowService NextStepInt(PerformerList performerList);
        IWorkflowService PrevStepInt();

        //流转相关接口
        //Flow related method
        WfExecutedResult Start();
        WfExecutedResult StartByMessage();
        WfExecutedResult Run();
        WfExecutedResult Withdraw();
        WfExecutedResult SendBack();
        WfExecutedResult Resend();
        WfExecutedResult Revise();
        WfExecutedResult Jump(JumpOptionEnum jumpOption = JumpOptionEnum.Default);
        WfExecutedResult Reverse();
        WfExecutedResult Reject();
        WfExecutedResult Close();
        WfExecutedResult SignForward();
        #endregion
    }
}
