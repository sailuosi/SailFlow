using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using sailflow.hub.Resource;
using sailflow.engine.Common;
using sailflow.data;
using mooSQL.data;
using sailflow.engine.Client;
using sailflow.engine.Entity;
using sailflow.engine.Manager;
using sailflow.engine.Xpdl;
using sailflow.engine.Runtime;
using sailflow.engine.Result;

namespace sailflow.engine.Service
{
    /// <summary>
    /// Workflow Service - Query Methods
    /// 工作流服务 - 查询方法
    /// </summary>
    public partial class WorkflowService : IWorkflowService
    {
        #region Get Next Activity Info
        /// <summary>
        /// Obtain the first feasible node of the process
        /// 获取流程的第一个可办理节点
        /// </summary>
        public Activity GetFirstActivity(string processId, string version)
        {
            var processModel = Sail.ProcessModelFactory.CreateByProcess(processId, version);
            var firstActivity = processModel.GetFirstActivity();
            return firstActivity;
        }

        /// <summary>
        /// Get the node list of task types
        /// 获取任务类型的节点列表
        /// </summary>
        public IList<Activity> GetTaskActivityList(string processId, string version)
        {
            var processModel = Sail.ProcessModelFactory.CreateByProcess(processId, version);
            var activityList = processModel.GetTaskActivityList();

            return activityList;
        }

        /// <summary>
        /// Get the node list of task types by definition-row primary key.
        /// 按定义行主键获取任务类型节点列表（勿传入流程设计定义键 ProcessId）。
        /// </summary>
        public IList<Activity> GetTaskActivityListByProcessDefId(string processDefId)
        {
            var pm = Sail.ManagerFactory.Processes;
            var entity = pm.GetById(processDefId);
            var processModel = Sail.ProcessModelFactory.CreateByProcess(entity);
            var activityList = processModel.GetTaskActivityList();

            return activityList;
        }

        /// <summary>
        /// Get a list of nodes for all task types (including countersignature and subprocesses)
        /// 获取全部任务类型的节点列表（包含会签和子流程）
        /// </summary>
        public IList<Activity> GetAllTaskActivityList(string processId, string version)
        {
            var processModel = Sail.ProcessModelFactory.CreateByProcess(processId, version);
            var activityList = processModel.GetAllTaskActivityList();

            return activityList;
        }

        /// <summary>
        /// Get the next node information of the current node
        /// 获取当前节点的下一个节点信息
        /// </summary>
        public Activity GetNextActivity(string processId, 
            string version, 
            string activityId)
        {
            var processModel = Sail.ProcessModelFactory.CreateByProcess(processId, version);
            var nextActivity = processModel.GetNextActivity(activityId);
            return nextActivity;
        }

        /// <summary>
        /// Get the next node information of the current node
        /// 获取当前节点的下一个节点信息
        /// </summary>
        public IList<NodeView> GetNextActivityTree(String processId,
            String version,
            String activityId,
            IDictionary<string, string> condition)
        {
            var processModel = Sail.ProcessModelFactory.CreateByProcess(processId, version);
            using (var kit = Sail.UseRunSQL())
            {
                var nextResult = processModel.GetNextActivityListView(activityId, null, condition, kit);

                return nextResult.StepList;
            }
        }

        /// <summary>
        /// Simple mode: Obtain the next node of the process based on the application 
        /// (without considering the situation of multiple subsequent nodes)
        /// 简单模式：根据应用获取流程下一步节点(不考虑有多个后续节点的情况）
        /// </summary>
        public NodeView GetNextActivity(FlowCommandContext runner,
            IDictionary<string, string> condition = null)
        {
            var info = GetNextStepInfo(runner, condition);
            return info?.NextActivityRoleUserTree?.FirstOrDefault();
        }

        /// <summary>
        /// Simple mode: Obtain the next node of the process based on the application 
        /// (without considering the situation of multiple subsequent nodes)
        /// 简单模式：根据应用获取流程下一步节点(不考虑有多个后续节点的情况）
        /// </summary>
        public NodeView GetNextActivity(string taskId,
            IDictionary<string, string> condition = null)
        {
            var runner = new FlowCommandContext { TaskId = taskId };
            var info = GetNextStepInfo(runner, condition);
            return info?.NextActivityRoleUserTree?.FirstOrDefault();
        }

        /// <summary>
        /// Obtain the next node list according to the application process
        /// 根据应用获取流程下一步节点列表
        /// </summary>
        public IList<NodeView> GetNextActivityTree(FlowCommandContext runner, 
            IDictionary<string, string> condition = null)
        {
            var info = GetNextStepInfo(runner, condition);
            return info?.NextActivityRoleUserTree ?? new List<NodeView>();
        }

        /// <summary>
        /// Obtain the next node list according to the application process
        /// 根据应用获取流程下一步节点列表
        /// </summary>
        public async Task<IList<NodeView>> GetNextActivityTreeAsync(FlowCommandContext runner,
            IDictionary<string, string> condition = null, CancellationToken cancellationToken = default)
        {
            var info = await GetNextStepInfoAsync(runner, condition, cancellationToken).ConfigureAwait(false);
            return info?.NextActivityRoleUserTree ?? new List<NodeView>();
        }

        /// <summary>
        /// Obtain the next node list according to the application process
        /// 获取下一步活动列表树
        /// </summary>
        public IList<NodeView> GetNextActivityTree(string taskId,
            IDictionary<string, string> condition = null)
        {
            var runner = new FlowCommandContext { TaskId = taskId };
            var info = GetNextStepInfo(runner, condition);
            return info?.NextActivityRoleUserTree ?? new List<NodeView>();
        }

        /// <summary>
        /// Obtain the next node list according to the application process
        /// 获取下一步活动列表树
        /// </summary>
        public async Task<IList<NodeView>> GetNextActivityTreeAsync(string taskId,
            IDictionary<string, string> condition = null, CancellationToken cancellationToken = default)
        {
            var runner = new FlowCommandContext { TaskId = taskId };
            var info = await GetNextStepInfoAsync(runner, condition, cancellationToken).ConfigureAwait(false);
            return info?.NextActivityRoleUserTree ?? new List<NodeView>();
        }

        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 根据应用获取流程下一步活动列表
        /// </summary>
        public IList<Activity> GetNextActivityList(FlowCommandContext runner,
            IDictionary<string, string> condition = null)
        {
            var info = GetNextStepInfo(runner, condition);
            return ConvertNodeViewListToActivityList(info?.NextActivityRoleUserTree, runner.ProcessId, runner.Version);
        }

        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 根据应用获取流程下一步活动列表
        /// </summary>
        public async Task<IList<Activity>> GetNextActivityListAsync(FlowCommandContext runner,
            IDictionary<string, string> condition = null, CancellationToken cancellationToken = default)
        {
            var info = await GetNextStepInfoAsync(runner, condition, cancellationToken).ConfigureAwait(false);
            return ConvertNodeViewListToActivityList(info?.NextActivityRoleUserTree, runner.ProcessId, runner.Version);
        }

        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 获取下一步活动列表
        /// </summary>
        public IList<Activity> GetNextActivityList(string taskId,
            IDictionary<string, string> condition = null)
        {
            var runner = new FlowCommandContext { TaskId = taskId };
            var info = GetNextStepInfo(runner, condition);
            return ConvertNodeViewListToActivityList(info?.NextActivityRoleUserTree, runner.ProcessId, runner.Version);
        }
        
        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 获取下一步活动列表
        /// </summary>
        public async Task<IList<Activity>> GetNextActivityListAsync(string taskId,
            IDictionary<string, string> condition = null, CancellationToken cancellationToken = default)
        {
            var runner = new FlowCommandContext { TaskId = taskId };
            var info = await GetNextStepInfoAsync(runner, condition, cancellationToken).ConfigureAwait(false);
            return ConvertNodeViewListToActivityList(info?.NextActivityRoleUserTree, runner.ProcessId, runner.Version);
        }
        
        /// <summary>
        /// Convert NodeView list to Activity list
        /// 将 NodeView 列表转换为 Activity 列表
        /// </summary>
        private IList<Activity> ConvertNodeViewListToActivityList(IList<NodeView> nodeViewList, string processId, string version)
        {
            if (nodeViewList == null || nodeViewList.Count == 0)
            {
                return new List<Activity>();
            }
            
            if (string.IsNullOrEmpty(processId) || string.IsNullOrEmpty(version))
            {
                return new List<Activity>();
            }
            
            var activityList = new List<Activity>();
            
            try
            {
                var processModel = Sail.ProcessModelFactory.CreateByProcess(processId, version);
                
                // Extract unique ActivityId from NodeView list
                var activityIds = nodeViewList.Select(nv => nv.ActivityId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();
                
                // Convert each ActivityId to Activity object
                foreach (var activityId in activityIds)
                {
                    try
                    {
                        var activity = processModel.GetActivity(activityId);
                        if (activity != null)
                        {
                            activityList.Add(activity);
                        }
                    }
                    catch
                    {
                        // Skip invalid activity IDs
                    }
                }
            }
            catch
            {
                // Return empty list if ProcessModel creation fails
            }
            
            return activityList;
        }

        /// <summary>
        /// According to the application, obtain the next node list of the process, including role users
        /// 根据应用获取流程下一步节点列表，包含角色用户
        /// </summary>
        public IList<NodeView> GetNextActivityRoleUserTree(FlowCommandContext runner, 
            IDictionary<string, string> condition = null)
        {
            var info = GetNextStepInfo(runner, condition);
            return info?.NextActivityRoleUserTree ?? new List<NodeView>();
        }

        /// <summary>
        /// According to the application, obtain the next node list of the process, including role users
        /// 根据应用获取流程下一步节点列表，包含角色用户
        /// </summary>
        public async Task<IList<NodeView>> GetNextActivityRoleUserTreeAsync(FlowCommandContext runner,
            IDictionary<string, string> condition = null, CancellationToken cancellationToken = default)
        {
            var info = await GetNextStepInfoAsync(runner, condition, cancellationToken).ConfigureAwait(false);
            return info?.NextActivityRoleUserTree ?? new List<NodeView>();
        }

        /// <summary>
        /// According to the application, obtain the next node list of the process, including role users
        /// Includes:
        /// 1) The next step for the gateway is to add a pre loaded user list of personnel;
        /// 2) The next step in the co signing mode is to add a pre loaded user list of personnel;
        /// 根据应用获取流程下一步节点列表，包含角色用户
        /// 包含：
        /// 1) 网关下一步添加人员的预加载用户列表；
        /// 2) 会签模式的下一步添加人员的预加载用户列表；
        /// </summary>
        public NextStepInfo GetNextStepInfo(FlowCommandContext runner, 
            IDictionary<string, string> condition = null)
        {
            var nsp = Managers.NextSteps;
            var nextStepInfo = nsp.GetNextStepInfo(ResourceService, runner, condition);

            return nextStepInfo;
        }

        /// <summary>
        /// Asynchronous retrieval of next step list
        /// 异步获取下一步列表
        /// </summary>
        public Task<NextStepInfo> GetNextStepInfoAsync(FlowCommandContext runner,
            IDictionary<string, string> condition = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(GetNextStepInfo(runner, condition));
        }

        /// <summary>
        /// Get the list of node roles and personnel for the next step of the starting node when the process starts
        /// Unified integration into: GetNextActivityRoleUserTree()
        /// 流程启动时获取开始节点下一步的节点角色人员列表
        /// 统一整合到: GetNextActivityRoleUserTree()
        /// </summary>
        public IList<NodeView> GetFirstActivityRoleUserTree(FlowCommandContext runner, 
            IDictionary<string, string> condition = null)
        {
            using (var kit = Sail.UseRunSQL())
            {
                var processModel = Sail.ProcessModelFactory.CreateByProcess(runner.ProcessId, runner.Version);
                // 与启动运行时一致：从开始事件取下一步（首个办理节点）
                var startActivity = processModel.GetStartActivity();
                var nextResult = processModel.GetNextActivityListView(startActivity.ActivityId,
                    null,
                    condition,
                    kit);

                foreach (var ns in nextResult.StepList)
                {
                    var roleIDs = ns.Roles != null ? ns.Roles.Select(x => x.Id).ToArray() : Array.Empty<string>();
                    if (roleIDs.Length > 0)
                        ns.Users = ResourceService.GetUserListByRoleReceiverType(roleIDs, runner.UserId, (int)ns.ReceiverType);
                }
                return nextResult.StepList;
            }
        }

        /// <summary>
        /// Get the list of node roles and personnel for the next step of the starting node when the process starts
        /// Unified integration into: GetNextActivityRoleUserTree()
        /// 流程启动时获取开始节点下一步的节点角色人员列表
        /// 统一整合到: GetNextActivityRoleUserTree()
        /// </summary>
        public Task<IList<NodeView>> GetFirstActivityRoleUserTreeAsync(FlowCommandContext runner,
            IDictionary<string, string> condition = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(GetFirstActivityRoleUserTree(runner, condition));
        }

        /// <summary>
        /// Get activity entity
        /// 获取当前活动实体
        /// </summary>
        public Activity GetActivityEntity(string processId, string version, string activityId)
        {
            var processModel = Sail.ProcessModelFactory.CreateByProcess(processId, version);
            return processModel.GetActivity(activityId);
        }

        /// <summary>
        /// Get activity roles
        /// 获取活动节点下的角色数据
        /// </summary>
        public IList<Role> GetActivityRoles(string processId, string version, string activityId)
        {
            var processModel = Sail.ProcessModelFactory.CreateByProcess(processId, version);
            return processModel.GetActivityRoles(activityId);
        }
        #endregion

        #region Get Previous Activity Info
        /// <summary>
        /// Get previous activity list
        /// 获取上一步节点信息
        /// </summary>
        public IList<NodeView> GetPreviousActivityTree(FlowCommandContext runner)
        {
            var psc = Managers.PreviousSteps;
            var hasGatewayPassed = false;
            var nodeList = psc.GetPreviousActivityTree(runner, out hasGatewayPassed);

            return nodeList;
        }

        /// <summary>
        /// Get previous activity list
        /// 获取上一步节点信息
        /// </summary>
        public Task<IList<NodeView>> GetPreviousActivityTreeAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(GetPreviousActivityTree(runner));
        }

        /// <summary>
        /// Get previous activity list
        /// 获取上一步节点信息
        /// </summary>
        public PreviousStepInfo GetPreviousStepInfo(FlowCommandContext runner)
        {             
            var hasGatewayPassed = false;
            var psc = Managers.PreviousSteps;
            var nodeList = psc.GetPreviousActivityTree(runner, out hasGatewayPassed);
            var psi = new PreviousStepInfo();
            psi.PreviousActivityRoleUserTree = nodeList;
            psi.HasGatewayPassed = hasGatewayPassed;

            return psi;
        }

        /// <summary>
        /// Get previous activity list
        /// 异步获取上一步节点信息
        /// </summary>
        public Task<PreviousStepInfo> GetPreviousStepInfoAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(GetPreviousStepInfo(runner));
        }
        #endregion

        #region Get Sign Froward Step Info
        /// <summary>
        /// Obtain performer information for the signing process
        /// 获取加签步骤人员信息
        /// </summary>
        public SignForwardStepInfo GetSignForwardStepInfo(FlowCommandContext runner)
        {
            var ssp = Managers.SignForwardSteps;
            var stepInfo = ssp.GetSignForwardStepInfo(runner);

            return stepInfo;
        }
        #endregion

        #region Process Instance Query
        /// <summary>
        /// Get process instance by id
        /// 获取流程实例数据
        /// </summary>
        public ProcessInstanceEntity GetProcessInstance(string processInstanceId)
        {
            var pim = Managers.ProcessInstances;
            var instance = pim.GetById(processInstanceId);
            return instance;
        }

        /// <summary>
        /// Get process instance by app instance id
        /// 获取流程实例数据
        /// </summary>
        public IList<ProcessInstanceEntity> GetProcessInstanceListByBizPK(string bizPK)
        {
            var pim = Managers.ProcessInstances;
            var list = pim.GetProcessInstance(bizPK).ToList();
            return list;
        }

        /// <summary>
        /// Get process instance by activity instance id
        /// 获取流程实例数据
        /// </summary>
        public ProcessInstanceEntity GetProcessInstanceByActivity(string activityInstanceId)
        {
            var pim = Managers.ProcessInstances;
            return pim.GetByActivity(activityInstanceId);
        }

        /// <summary>
        /// Get process instance by runner
        /// 获取运行中的流程实例
        /// </summary>
        public ProcessInstanceEntity GetRunningProcessInstance(FlowCommandContext runner)
        {
            var pim = Managers.ProcessInstances;
            var entity = pim.GetRunningProcessInstance(runner.BizPK, runner.ProcessId);
            return entity;
        }

        /// <summary>
        /// Get process initiator
        /// 获取流程发起人信息
        /// </summary>
        public User GetProcessInitiator(string processInstanceId)
        {
            User initiator = null;
            try
            {
                var pim = Managers.ProcessInstances;
                initiator = pim.GetProcessInitiator(processInstanceId);
            }
            catch
            {
                throw;
            }
            return initiator;
        }

        /// <summary>
        /// Get activity instance
        /// 获取活动实例数据
        /// </summary>
        public ActivityInstanceEntity GetActivityInstance(string activityInstanceId)
        {
            var aim = Sail.ManagerFactory.Activities;
            var instance = aim.GetById(activityInstanceId);
            return instance;
        }

        /// <summary>
        /// Get activity instance
        /// 获取一个流程实例下的所有活动实例
        /// </summary>
        public IList<ActivityInstanceEntity> GetActivityInstanceList(string processInstanceId)
        {
            var aim = Sail.ManagerFactory.Activities;
            using (var kit = Sail.UseRunSQL())
            {
                return aim.GetActivityInstanceList(kit, processInstanceId);
            }
        }

        /// <summary>
        /// Get target activity instance list
        /// 获取目标活动实例列表
        /// </summary>
        public IList<ActivityInstanceEntity> GetTargetActivityInstanceList(string fromActivityInstanceId)
        {
            var tim = Managers.Transitions;
            return tim.GetTargetActivityInstanceList(fromActivityInstanceId);
        }

        /// <summary>
        /// Entrust task
        /// 创建新的委托任务
        /// </summary>
        public WfDataManagedResult EntrustTask(TaskEntrustedEntity entrusted, bool cancalOriginalTask = true)
        {
            var tm = Sail.ManagerFactory.Tasks;
            return tm.Entrust(entrusted, cancalOriginalTask);
        }

        public void CancelEntrustedTask(string id)
        {
            var tm = Sail.ManagerFactory.Tasks;
            tm.CancelEntrustedTask(id);
        }

        /// <summary>
        /// Replace task
        /// 取代任务
        /// </summary>
        public Boolean ReplaceTask(string taskId, List<TaskReplacedEntity> replaced, FlowCommandContext runner)
        {
            var tm = Sail.ManagerFactory.Tasks;
            return tm.Replace(taskId, replaced, runner);
        }

        /// <summary>
        /// Set process overdue
        /// 设置流程实例的过期时间
        /// </summary>
        public Boolean SetProcessOverdue(string processInstanceId, DateTime overdueDateTime, FlowCommandContext runner)
        {
            var pim = Managers.ProcessInstances;
            return pim.SetOverdue(processInstanceId, overdueDateTime, runner);
        }

        /// <summary>
        /// Set the scheduled job of the process instance to completion status
        /// (Used for HangFire backend polling task)
        /// 设置流程实例的定时作业为完成状态
        /// (用于HangFire后台轮询任务)
        /// </summary>
        public void SetProcessJobTimerCompleted(string processInstanceId)
        {
            var pim = Managers.ProcessInstances;
            var processInstance = pim.GetById(processInstanceId);
            processInstance.JobTimerStatus = (short)JobTimerStatusEnum.Completed;
            processInstance.JobTimerTreatedDateTime = System.DateTime.UtcNow;
            pim.Update(processInstance);
        }

        /// <summary>
        /// Set the scheduled job of the activity instance to completion status
        /// (Used for HangFire backend polling task)
        /// 设置活动实例的定时作业为完成状态
        /// (用于HangFire后台轮询任务)
        /// </summary>
        public void SetActivityJobTimerCompleted(string activityInstanceId)
        {
            var aim = Sail.ManagerFactory.Activities;
            var activityInstance = aim.GetById(activityInstanceId);
            activityInstance.JobTimerStatus = (short)JobTimerStatusEnum.Completed;
            activityInstance.JobTimerTreatedDateTime = System.DateTime.UtcNow;
            aim.Update(activityInstance);
        }

        /// <summary>
        /// Get running node
        /// 获取流程当前运行节点信息
        /// </summary>
        public ActivityInstanceEntity GetRunningNode(FlowCommandContext runner)
        {
            var aim = Sail.ManagerFactory.Activities;
            var entity = aim.GetRunningNode(runner);

            return entity;
        }

        /// <summary>
        /// Determine if it is my task
        /// 判断是否是我的任务
        /// </summary>
        public bool IsMineTask(ActivityInstanceEntity entity, string userId)
        {
            var aim = Sail.ManagerFactory.Activities;
            bool isMine = aim.IsMineTask(entity, userId);
            return isMine;
        }

        /// <summary>
        /// Get process variable
        /// 获取流程变量
        /// </summary>
        public ProcessVariableEntity GetProcessVariable(string processId, string variableId)
        {
            var pvm = Managers.ProcessVariables;
            var entity = pvm.GetVariableById(processId, variableId);

            return entity;
        }

        /// <summary>
        /// Get process variable
        /// 获取流程变量
        /// </summary>
        public ProcessVariableEntity GetProcessVariable(ProcessVariableQuery query)
        {
            var pvm = Managers.ProcessVariables;
            var entity = pvm.GetVariableEntity(query);
            return entity;
        }

        /// <summary>
        /// Get process variable list
        /// 获取变量列表
        /// </summary>
        public IList<ProcessVariableEntity> GetProcessVariableList(ProcessVariableQuery query)
        {
            var pvm = Managers.ProcessVariables;
            var list = pvm.GetVariableList(query.ProcessInstanceId);
            return list;
        }

        /// <summary>
        /// Delete process variable
        /// 删除流程变量
        /// </summary>
        public void DeleteProcessVariable(string processId, string variableId)
        {
            var pvm = Managers.ProcessVariables;
            pvm.DeleteVariable(processId, variableId);
        }

        /// <summary>
        /// Validate process variable
        /// 验证触发表达式是否满足
        /// </summary>
        public Boolean ValidateProcessVariable(string processInstanceId, string expression)
        {
            var pvm = Managers.ProcessVariables;
            return pvm.ValidateProcessVariable(processInstanceId, expression);
        }

        /// <summary>
        /// Update process instance
        /// 更新流程实例实体
        /// </summary>
        public void UpdateProcessInstance(ProcessInstanceEntity entity)
        {
            var pim = Managers.ProcessInstances;
            pim.Update(entity);
        }

        /// <summary>
        /// Save process variable
        /// 保存流程变量
        /// </summary>
        public string SaveProcessVariable(ProcessVariableEntity entity)
        {
            var pvm = Managers.ProcessVariables;
            var entityId = pvm.SaveVariable(entity);
            return entityId;
        }

        /// <summary>
        /// 获取流程变量定义列表（wf_variable）
        /// </summary>
        public IList<VariableEntity> GetVariableDefinitionList(string processId, string version, string activityId)
        {
            var vm = Managers.Variables;
            return vm.GetList(processId, version, activityId);
        }

        /// <summary>
        /// 获取单条流程变量定义
        /// </summary>
        public VariableEntity GetVariableDefinition(string id)
        {
            var vm = Managers.Variables;
            return vm.GetById(id);
        }

        /// <summary>
        /// 保存单条流程变量定义
        /// </summary>
        public string SaveVariableDefinition(VariableEntity entity)
        {
            var vm = Managers.Variables;
            if (!string.IsNullOrWhiteSpace(entity.VariableId))
            {
                vm.Update(entity);
                return entity.VariableId;
            }
            return vm.Insert(entity);
        }

        /// <summary>
        /// 删除流程变量定义
        /// </summary>
        public void DeleteVariableDefinition(string id)
        {
            var vm = Managers.Variables;
            vm.Delete(id);
        }

        /// <summary>
        /// 按节点保存流程变量定义（先删后增）
        /// </summary>
        public void SaveVariableDefinitionList(string processId, string version, string activityId, IList<VariableEntity> list)
        {
            var vm = Managers.Variables;
            vm.SaveList(processId, version, activityId, list);
        }

        /// <summary>
        /// 获取规则集列表（wf_rule_set）
        /// </summary>
        public IList<RuleSetEntity> GetRuleSetList()
        {
            var rm = Managers.RuleSets;
            return rm.GetAll();
        }

        /// <summary>
        /// 获取单个规则集
        /// </summary>
        public RuleSetEntity GetRuleSet(string ruleSetCode)
        {
            var rm = Managers.RuleSets;
            return rm.GetByCode(ruleSetCode);
        }

        /// <summary>
        /// 保存规则集（按 code 更新/新增）
        /// </summary>
        public string SaveRuleSet(RuleSetEntity entity)
        {
            var rm = Managers.RuleSets;
            var exist = rm.GetByCode(entity?.RuleSetCode ?? "");
            if (exist != null)
            {
                entity.RuleSetId = exist.RuleSetId;
                rm.Update(entity);
                return entity.RuleSetId;
            }
            return rm.Insert(entity);
        }

        /// <summary>
        /// 删除规则集
        /// </summary>
        public void DeleteRuleSet(string ruleSetCode)
        {
            var rm = Managers.RuleSets;
            rm.DeleteByCode(ruleSetCode);
        }
        #endregion

        #region Task Read
        /// <summary>
        /// Set task to read status (retrieve task based on task Id)
        /// 设置任务为已读状态(根据任务ID获取任务)
        /// </summary>
        public bool SetTaskRead(FlowCommandContext runner)
        {
            bool isRead = false;
            try
            {
                var taskManager = Sail.ManagerFactory.Tasks;
                taskManager.SetTaskRead(runner);
                isRead = true;
            }
            catch (System.Exception)
            {
                throw;
            }

            return isRead;
        }

        /// <summary>
        /// Get task view
        /// 获取任务视图
        /// </summary>
        public TaskViewEntity GetTaskView(string taskId)
        {
            var tm = Sail.ManagerFactory.Tasks;
            var entity = tm.GetTaskView(taskId);
            return entity;
        }

        /// <summary>
        /// Get task view
        /// 获取任务视图
        /// </summary>
        public TaskViewEntity GetTaskView(string processInstanceId, string activityInstanceId)
        {
            var tm = Sail.ManagerFactory.Tasks;
            var entity = tm.GetTaskViewByActivity(processInstanceId, activityInstanceId);
            return entity;
        }

        /// <summary>
        /// Get task view
        /// 获取任务视图
        /// </summary>
        public TaskViewEntity GetTaskView(string bizPK, string processId, string userId)
        {
            var tm = Sail.ManagerFactory.Tasks;
            return tm.GetTaskOfMine(bizPK, processId, userId);
        }
        #endregion
    }
}
