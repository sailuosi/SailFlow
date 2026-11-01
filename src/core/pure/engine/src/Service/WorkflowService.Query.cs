using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using saiflow.hub.Resource;
using saiflow.engine.Common;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Client;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Xpdl;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Core.Parser;
using saiflow.engine.Business.Result;

namespace saiflow.engine.Service
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
            var processModel = ProcessModelFactory.CreateByProcess(Sail, processId, version);
            var firstActivity = processModel.GetFirstActivity();
            return firstActivity;
        }

        /// <summary>
        /// Get the node list of task types
        /// 获取任务类型的节点列表
        /// </summary>
        public IList<Activity> GetTaskActivityList(string processId, string version)
        {
            var processModel = ProcessModelFactory.CreateByProcess(Sail, processId, version);
            var activityList = processModel.GetTaskActivityList();

            return activityList;
        }

        /// <summary>
        /// Get the node list of task types
        /// 获取任务类型的节点列表
        /// </summary>
        public IList<Activity> GetTaskActivityList(int processId)
        {
            var pm = new ProcessManager(Sail);
            var entity = pm.GetById(processId);
            var processModel = ProcessModelFactory.CreateByProcess(Sail, entity);
            var activityList = processModel.GetTaskActivityList();

            return activityList;
        }

        /// <summary>
        /// Get a list of nodes for all task types (including countersignature and subprocesses)
        /// 获取全部任务类型的节点列表（包含会签和子流程）
        /// </summary>
        public IList<Activity> GetAllTaskActivityList(string processId, string version)
        {
            var processModel = ProcessModelFactory.CreateByProcess(Sail, processId, version);
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
            var processModel = ProcessModelFactory.CreateByProcess(Sail, processId, version);
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
            var processModel = ProcessModelFactory.CreateByProcess(Sail, processId, version);
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
        public NodeView GetNextActivity(WfAppRunner runner,
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
        public NodeView GetNextActivity(int taskId,
            IDictionary<string, string> condition = null)
        {
            var runner = new WfAppRunner { TaskId = taskId };
            var info = GetNextStepInfo(runner, condition);
            return info?.NextActivityRoleUserTree?.FirstOrDefault();
        }

        /// <summary>
        /// Obtain the next node list according to the application process
        /// 根据应用获取流程下一步节点列表
        /// </summary>
        public IList<NodeView> GetNextActivityTree(WfAppRunner runner, 
            IDictionary<string, string> condition = null)
        {
            var info = GetNextStepInfo(runner, condition);
            return info?.NextActivityRoleUserTree ?? new List<NodeView>();
        }

        /// <summary>
        /// Obtain the next node list according to the application process
        /// 根据应用获取流程下一步节点列表
        /// </summary>
        public async Task<IList<NodeView>> GetNextActivityTreeAsync(WfAppRunner runner, 
            IDictionary<string, string> condition = null)
        {
            var info = await GetNextStepInfoAsync(runner, condition);
            return info?.NextActivityRoleUserTree ?? new List<NodeView>();
        }

        /// <summary>
        /// Obtain the next node list according to the application process
        /// 获取下一步活动列表树
        /// </summary>
        public IList<NodeView> GetNextActivityTree(int taskId,
            IDictionary<string, string> condition = null)
        {
            var runner = new WfAppRunner { TaskId = taskId };
            var info = GetNextStepInfo(runner, condition);
            return info?.NextActivityRoleUserTree ?? new List<NodeView>();
        }

        /// <summary>
        /// Obtain the next node list according to the application process
        /// 获取下一步活动列表树
        /// </summary>
        public async Task<IList<NodeView>> GetNextActivityTreeAsync(int taskId, 
            IDictionary<string, string> condition = null)
        {
            var runner = new WfAppRunner { TaskId = taskId };
            var info = await GetNextStepInfoAsync(runner, condition);
            return info?.NextActivityRoleUserTree ?? new List<NodeView>();
        }

        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 根据应用获取流程下一步活动列表
        /// </summary>
        public IList<Activity> GetNextActivityList(WfAppRunner runner,
            IDictionary<string, string> condition = null)
        {
            var info = GetNextStepInfo(runner, condition);
            return ConvertNodeViewListToActivityList(info?.NextActivityRoleUserTree, runner.ProcessId, runner.Version);
        }

        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 根据应用获取流程下一步活动列表
        /// </summary>
        public async Task<IList<Activity>> GetNextActivityListAsync(WfAppRunner runner,
            IDictionary<string, string> condition = null)
        {
            var info = await GetNextStepInfoAsync(runner, condition);
            return ConvertNodeViewListToActivityList(info?.NextActivityRoleUserTree, runner.ProcessId, runner.Version);
        }

        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 获取下一步活动列表
        /// </summary>
        public IList<Activity> GetNextActivityList(int taskId,
            IDictionary<string, string> condition = null)
        {
            var runner = new WfAppRunner { TaskId = taskId };
            var info = GetNextStepInfo(runner, condition);
            return ConvertNodeViewListToActivityList(info?.NextActivityRoleUserTree, runner.ProcessId, runner.Version);
        }
        
        /// <summary>
        /// Obtain the next activity list according to the application process
        /// 获取下一步活动列表
        /// </summary>
        public async Task<IList<Activity>> GetNextActivityListAsync(int taskId, 
            IDictionary<string, string> condition = null)
        {
            var runner = new WfAppRunner { TaskId = taskId };
            var info = await GetNextStepInfoAsync(runner, condition);
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
                var processModel = ProcessModelFactory.CreateByProcess(Sail, processId, version);
                
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
        public IList<NodeView> GetNextActivityRoleUserTree(WfAppRunner runner, 
            IDictionary<string, string> condition = null)
        {
            var info = GetNextStepInfo(runner, condition);
            return info?.NextActivityRoleUserTree ?? new List<NodeView>();
        }

        /// <summary>
        /// According to the application, obtain the next node list of the process, including role users
        /// 根据应用获取流程下一步节点列表，包含角色用户
        /// </summary>
        public async Task<IList<NodeView>> GetNextActivityRoleUserTreeAsync(WfAppRunner runner, 
            IDictionary<string, string> condition = null)
        {
            var info = await GetNextStepInfoAsync(runner, condition);
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
        public NextStepInfo GetNextStepInfo(WfAppRunner runner, 
            IDictionary<string, string> condition = null)
        {
            var nsp = new NextStepParser(Sail);
            var nextStepInfo = nsp.GetNextStepInfo(ResourceService, runner, condition);

            return nextStepInfo;
        }

        /// <summary>
        /// Asynchronous retrieval of next step list
        /// 异步获取下一步列表
        /// </summary>
        public async Task<NextStepInfo> GetNextStepInfoAsync(WfAppRunner runner, 
            IDictionary<string, string> condition = null)
        {
            var task = await Task.Run<NextStepInfo>(() =>
            {
                return GetNextStepInfo(runner, condition);
            });
            return task;
        }

        /// <summary>
        /// Get the list of node roles and personnel for the next step of the starting node when the process starts
        /// Unified integration into: GetNextActivityRoleUserTree()
        /// 流程启动时获取开始节点下一步的节点角色人员列表
        /// 统一整合到: GetNextActivityRoleUserTree()
        /// </summary>
        public IList<NodeView> GetFirstActivityRoleUserTree(WfAppRunner runner, 
            IDictionary<string, string> condition = null)
        {
            using (var kit = Sail.UseRunSQL())
            {
                var processModel = ProcessModelFactory.CreateByProcess(Sail, runner.ProcessId, runner.Version);
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
        public async Task<IList<NodeView>> GetFirstActivityRoleUserTreeAsync(WfAppRunner runner, 
            IDictionary<string, string> condition = null)
        {
            var task = await Task.Run<IList<NodeView>>(() =>
            {
                return GetFirstActivityRoleUserTree(runner, condition);
            });
            return task;
        }

        /// <summary>
        /// Get activity entity
        /// 获取当前活动实体
        /// </summary>
        public Activity GetActivityEntity(string processId, string version, string activityId)
        {
            var processModel = ProcessModelFactory.CreateByProcess(Sail, processId, version);
            return processModel.GetActivity(activityId);
        }

        /// <summary>
        /// Get activity roles
        /// 获取活动节点下的角色数据
        /// </summary>
        public IList<Role> GetActivityRoles(string processId, string version, string activityId)
        {
            var processModel = ProcessModelFactory.CreateByProcess(Sail, processId, version);
            return processModel.GetActivityRoles(activityId);
        }
        #endregion

        #region Get Previous Activity Info
        /// <summary>
        /// Get previous activity list
        /// 获取上一步节点信息
        /// </summary>
        public IList<NodeView> GetPreviousActivityTree(WfAppRunner runner)
        {
            var psc = new PreviousStepChecker(Sail);
            var hasGatewayPassed = false;
            var nodeList = psc.GetPreviousActivityTree(runner, out hasGatewayPassed);

            return nodeList;
        }

        /// <summary>
        /// Get previous activity list
        /// 获取上一步节点信息
        /// </summary>
        public async Task<IList<NodeView>> GetPreviousActivityTreeAsync(WfAppRunner runner)
        {
            var task = await Task.Run<IList<NodeView>>(() =>
            {
                return GetPreviousActivityTree(runner);
            });
            return task;
        }

        /// <summary>
        /// Get previous activity list
        /// 获取上一步节点信息
        /// </summary>
        public PreviousStepInfo GetPreviousStepInfo(WfAppRunner runner)
        {             
            var hasGatewayPassed = false;
            var psc = new PreviousStepChecker(Sail);
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
        public async Task<PreviousStepInfo> GetPreviousStepInfoAsync(WfAppRunner runner)
        {
            var task = await Task.Run<PreviousStepInfo>(() =>
            {
                return GetPreviousStepInfo(runner);
            });
            return task;
        }
        #endregion

        #region Get Sign Froward Step Info
        /// <summary>
        /// Obtain performer information for the signing process
        /// 获取加签步骤人员信息
        /// </summary>
        public SignForwardStepInfo GetSignForwardStepInfo(WfAppRunner runner)
        {
            var ssp = new SignForwardStepMaker(Sail);
            var stepInfo = ssp.GetSignForwardStepInfo(runner);

            return stepInfo;
        }
        #endregion

        #region Process Instance Query
        /// <summary>
        /// Get process instance by id
        /// 获取流程实例数据
        /// </summary>
        public ProcessInstanceEntity GetProcessInstance(int processInstanceId)
        {
            var pim = new ProcessInstanceManager(Sail);
            var instance = pim.GetById(processInstanceId);
            return instance;
        }

        /// <summary>
        /// Get process instance by app instance id
        /// 获取流程实例数据
        /// </summary>
        public IList<ProcessInstanceEntity> GetProcessInstance(string appInstanceId)
        {
            var pim = new ProcessInstanceManager(Sail);
            var list = pim.GetProcessInstance(appInstanceId).ToList();
            return list;
        }

        /// <summary>
        /// Get process instance by activity instance id
        /// 获取流程实例数据
        /// </summary>
        public ProcessInstanceEntity GetProcessInstanceByActivity(int activityInstanceId)
        {
            var pim = new ProcessInstanceManager(Sail);
            return pim.GetByActivity(activityInstanceId);
        }

        /// <summary>
        /// Get process instance by runner
        /// 获取运行中的流程实例
        /// </summary>
        public ProcessInstanceEntity GetRunningProcessInstance(WfAppRunner runner)
        {
            var pim = new ProcessInstanceManager(Sail);
            var entity = pim.GetRunningProcessInstance(runner.AppInstanceId, runner.ProcessId);
            return entity;
        }

        /// <summary>
        /// Get process initiator
        /// 获取流程发起人信息
        /// </summary>
        public User GetProcessInitiator(int processInstanceId)
        {
            User initiator = null;
            try
            {
                var pim = new ProcessInstanceManager(Sail);
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
        public ActivityInstanceEntity GetActivityInstance(int activityInstanceId)
        {
            var aim = new ActivityInstanceManager(Sail);
            var instance = aim.GetById(activityInstanceId);
            return instance;
        }

        /// <summary>
        /// Get activity instance
        /// 获取一个流程实例下的所有活动实例
        /// </summary>
        public IList<ActivityInstanceEntity> GetActivityInstanceList(int processInstanceId)
        {
            var aim = new ActivityInstanceManager(Sail);
            using (var kit = Sail.UseRunSQL())
            {
                return aim.GetActivityInstanceList(kit, processInstanceId);
            }
        }

        /// <summary>
        /// Get target activity instance list
        /// 获取目标活动实例列表
        /// </summary>
        public IList<ActivityInstanceEntity> GetTargetActivityInstanceList(int fromActivityInstanceId)
        {
            var tim = new TransitionInstanceManager(Sail);
            return tim.GetTargetActivityInstanceList(fromActivityInstanceId);
        }

        /// <summary>
        /// Entrust task
        /// 创建新的委托任务
        /// </summary>
        public WfDataManagedResult EntrustTask(TaskEntrustedEntity entrusted, bool cancalOriginalTask = true)
        {
            var tm = new TaskManager(Sail);
            return tm.Entrust(entrusted, cancalOriginalTask);
        }

        public void CancelEntrustedTask(int id)
        {
            var tm = new TaskManager(Sail);
            tm.CancelEntrustedTask(id);
        }

        /// <summary>
        /// Replace task
        /// 取代任务
        /// </summary>
        public Boolean ReplaceTask(int taskId, List<TaskReplacedEntity> replaced, WfAppRunner runner)
        {
            var tm = new TaskManager(Sail);
            return tm.Replace(taskId, replaced, runner);
        }

        /// <summary>
        /// Set process overdue
        /// 设置流程实例的过期时间
        /// </summary>
        public Boolean SetProcessOverdue(int processInstanceId, DateTime overdueDateTime, WfAppRunner runner)
        {
            var pim = new ProcessInstanceManager(Sail);
            return pim.SetOverdue(processInstanceId, overdueDateTime, runner);
        }

        /// <summary>
        /// Set the scheduled job of the process instance to completion status
        /// (Used for HangFire backend polling task)
        /// 设置流程实例的定时作业为完成状态
        /// (用于HangFire后台轮询任务)
        /// </summary>
        public void SetProcessJobTimerCompleted(int processInstanceId)
        {
            var pim = new ProcessInstanceManager(Sail);
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
        public void SetActivityJobTimerCompleted(int activityInstanceId)
        {
            var aim = new ActivityInstanceManager(Sail);
            var activityInstance = aim.GetById(activityInstanceId);
            activityInstance.JobTimerStatus = (short)JobTimerStatusEnum.Completed;
            activityInstance.JobTimerTreatedDateTime = System.DateTime.UtcNow;
            aim.Update(activityInstance);
        }

        /// <summary>
        /// Get running node
        /// 获取流程当前运行节点信息
        /// </summary>
        public ActivityInstanceEntity GetRunningNode(WfAppRunner runner)
        {
            var aim = new ActivityInstanceManager(Sail);
            var entity = aim.GetRunningNode(runner);

            return entity;
        }

        /// <summary>
        /// Determine if it is my task
        /// 判断是否是我的任务
        /// </summary>
        public bool IsMineTask(ActivityInstanceEntity entity, string userId)
        {
            var aim = new ActivityInstanceManager(Sail);
            bool isMine = aim.IsMineTask(entity, userId);
            return isMine;
        }

        /// <summary>
        /// Get process variable
        /// 获取流程变量
        /// </summary>
        public ProcessVariableEntity GetProcessVariable(int variableId)
        {
            var pvm = new ProcessVariableManager(Sail);
            var entity = pvm.GetVariableById(variableId);

            return entity;
        }

        /// <summary>
        /// Get process variable
        /// 获取流程变量
        /// </summary>
        public ProcessVariableEntity GetProcessVariable(ProcessVariableQuery query)
        {
            var pvm = new ProcessVariableManager(Sail);
            var entity = pvm.GetVariableEntity(query);
            return entity;
        }

        /// <summary>
        /// Get process variable list
        /// 获取变量列表
        /// </summary>
        public IList<ProcessVariableEntity> GetProcessVariableList(ProcessVariableQuery query)
        {
            var pvm = new ProcessVariableManager(Sail);
            var list = pvm.GetVariableList(query.ProcessInstanceId);
            return list;
        }

        /// <summary>
        /// Delete process variable
        /// 删除流程变量
        /// </summary>
        public void DeleteProcessVariable(int variableId)
        {
            var pvm = new ProcessVariableManager(Sail);
            pvm.DeleteVariable(variableId);
        }

        /// <summary>
        /// Validate process variable
        /// 验证触发表达式是否满足
        /// </summary>
        public Boolean ValidateProcessVariable(int processInstanceId, string expression)
        {
            var pvm = new ProcessVariableManager(Sail);
            return pvm.ValidateProcessVariable(processInstanceId, expression);
        }

        /// <summary>
        /// Update process instance
        /// 更新流程实例实体
        /// </summary>
        public void UpdateProcessInstance(ProcessInstanceEntity entity)
        {
            var pim = new ProcessInstanceManager(Sail);
            pim.Update(entity);
        }

        /// <summary>
        /// Save process variable
        /// 保存流程变量
        /// </summary>
        public int SaveProcessVariable(ProcessVariableEntity entity)
        {
            var pvm = new ProcessVariableManager(Sail);
            var entityId = pvm.SaveVariable(entity);
            return entityId;
        }

        /// <summary>
        /// 获取流程变量定义列表（wf_variable）
        /// </summary>
        public IList<VariableEntity> GetVariableDefinitionList(string processId, string version, string activityId)
        {
            var vm = new VariableManager(Sail);
            return vm.GetList(processId, version, activityId);
        }

        /// <summary>
        /// 获取单条流程变量定义
        /// </summary>
        public VariableEntity GetVariableDefinition(int id)
        {
            var vm = new VariableManager(Sail);
            return vm.GetById(id);
        }

        /// <summary>
        /// 保存单条流程变量定义
        /// </summary>
        public int SaveVariableDefinition(VariableEntity entity)
        {
            var vm = new VariableManager(Sail);
            if (entity.Id != 0)
            {
                vm.Update(entity);
                return entity.Id.Value;
            }
            return vm.Insert(entity);
        }

        /// <summary>
        /// 删除流程变量定义
        /// </summary>
        public void DeleteVariableDefinition(int id)
        {
            var vm = new VariableManager(Sail);
            vm.Delete(id);
        }

        /// <summary>
        /// 按节点保存流程变量定义（先删后增）
        /// </summary>
        public void SaveVariableDefinitionList(string processId, string version, string activityId, IList<VariableEntity> list)
        {
            var vm = new VariableManager(Sail);
            vm.SaveList(processId, version, activityId, list);
        }

        /// <summary>
        /// 获取规则集列表（wf_rule_set）
        /// </summary>
        public IList<RuleSetEntity> GetRuleSetList()
        {
            var rm = new RuleSetManager(Sail);
            return rm.GetAll();
        }

        /// <summary>
        /// 获取单个规则集
        /// </summary>
        public RuleSetEntity GetRuleSet(string ruleSetCode)
        {
            var rm = new RuleSetManager(Sail);
            return rm.GetByCode(ruleSetCode);
        }

        /// <summary>
        /// 保存规则集（按 code 更新/新增）
        /// </summary>
        public int SaveRuleSet(RuleSetEntity entity)
        {
            var rm = new RuleSetManager(Sail);
            var exist = rm.GetByCode(entity?.RuleSetCode ?? "");
            if (exist != null)
            {
                entity.Id = exist.Id;
                rm.Update(entity);
                return entity.Id.Value;
            }
            return rm.Insert(entity);
        }

        /// <summary>
        /// 删除规则集
        /// </summary>
        public void DeleteRuleSet(string ruleSetCode)
        {
            var rm = new RuleSetManager(Sail);
            rm.DeleteByCode(ruleSetCode);
        }
        #endregion

        #region Task Read
        /// <summary>
        /// Set task to read status (retrieve task based on task Id)
        /// 设置任务为已读状态(根据任务ID获取任务)
        /// </summary>
        public bool SetTaskRead(WfAppRunner runner)
        {
            bool isRead = false;
            try
            {
                var taskManager = new TaskManager(Sail);
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
        public TaskViewEntity GetTaskView(int taskId)
        {
            var tm = new TaskManager(Sail);
            var entity = tm.GetTaskView(taskId);
            return entity;
        }

        /// <summary>
        /// Get task view
        /// 获取任务视图
        /// </summary>
        public TaskViewEntity GetTaskView(int processInstanceId, int activityInstanceId)
        {
            var tm = new TaskManager(Sail);
            var entity = tm.GetTaskViewByActivity(processInstanceId, activityInstanceId);
            return entity;
        }

        /// <summary>
        /// Get task view
        /// 获取任务视图
        /// </summary>
        public TaskViewEntity GetTaskView(string appInstanceId, string processId, string userId)
        {
            var tm = new TaskManager(Sail);
            return tm.GetTaskOfMine(appInstanceId, processId, userId);
        }
        #endregion
    }
}
