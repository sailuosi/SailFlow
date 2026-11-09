using System;
using System.Collections.Generic;
using System.Linq;
using saiflow.data;
using saiflow.hub.Localize;
using saiflow.hub.Resource;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.WebUtility;

using saiflow.engine.Client;
using saiflow.engine.Core;
using saiflow.engine.Factories;

namespace saiflow.engine.Runtime
{
    /// <summary>
    /// Next Step Parser
    /// 下一步的步骤列表读取类。
    ///
    /// 本解析器位于结构层路由 (IProcessModel.GetNextActivityTree / GetNextActivityList) 之上，
    /// 负责合并运行时条件、流程变量及资源服务 (IResourceService) 提供的执行人信息，
    /// 将“结构层”的下一步节点树转换为可直接被 WorkflowService / UI 使用的 NextStepInfo / NodeView 列表。
    /// </summary>
    internal class NextStepParser
    {
        public NextStepParser(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        /// <summary>
        /// Next step of the process: information acquisition
        /// 流程下一步信息获取
        /// </summary>
        /// <param name="resourceService"></param>
        /// <param name="runner"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        internal NextStepInfo GetNextStepInfo(IResourceService resourceService,
            FlowCommandContext runner, 
            IDictionary<string, string> condition = null)
        {
            NextStepInfo nextStepInfo = new NextStepInfo();
            var nextResult = GetNextActivityRoleUserTree(resourceService, runner, condition);
            if (nextResult == null)
                return nextStepInfo;

            nextStepInfo.Message = nextResult.Message;
            nextStepInfo.NextActivityRoleUserTree = nextResult.StepList;
            // 预选办理人依赖运行中 TaskView；发起前预览没有任务，跳过以免 NRE
            if (nextResult.StepList != null
                && nextResult.StepList.Count > 0
                && (runner.TaskId != null || !string.IsNullOrEmpty(runner.AppInstanceId)))
            {
                nextStepInfo.NextActivityPerformers = GetNextActivityPerformersPriliminary(runner);
            }

            return nextStepInfo;
        }

        /// <summary>
        /// Obtain the list of performer for the pre selection steps
        /// 获取预选步骤人员列表
        /// </summary>
        /// <param name="runner"></param>
        /// <returns></returns>
        private IDictionary<string, PerformerList> GetNextActivityPerformersPriliminary(FlowCommandContext runner)
        {
            IDictionary<string, PerformerList> nextSteps = null;

            var tm = Sail.ManagerFactory.Tasks;
            TaskViewEntity taskView = tm.GetTaskOfMine(runner);
            // 发起前 / 无我的任务：无可读的预选步骤
            if (taskView == null)
                return nextSteps;

            //读取活动实例中记录的步骤预选数据
            //Read the pre selected data of the steps recorded in the activity instance
            var aim = Sail.ManagerFactory.Activities;
            if (taskView.MainActivityInstanceId != null)
            {
                var mainActivityInstanceId = taskView.MainActivityInstanceId;
                var mainActivityInstance = aim.GetById(mainActivityInstanceId);
                if (mainActivityInstance != null)
                {
                    nextSteps = NextStepUtility.DeserializeNextStepPerformers(mainActivityInstance.NextStepPerformers, Sail.JsonSerializer);
                }
            }

            // 预选办理人：仅当「唯一出边」时尝试从网关/中间 Timer 实例读取。
            // 多出口不可调用 GetNextActivity（内部 SingleOrDefault），否则抛
            // “Sequence contains more than one matching element”，导致整次下一跳查询失败。
            // 多出口时由前端先选节点，再使用 StepList 上该节点的 Users。
            var processModel = Sail.ProcessModelFactory.CreateByTask(taskView);
            var forwardList = processModel.GetForwardTransitionList(taskView.ActivityId);
            if (forwardList == null || forwardList.Count != 1)
                return nextSteps;

            var nextActivity = forwardList[0]?.ToActivity;
            if (nextActivity != null)
            {
                if (nextActivity.ActivityType == ActivityTypeEnum.GatewayNode)
                {
                    //获取网关节点信息
                    //Obtain gateway node information
                    var gatewayActivityInstance = aim.GetActivityInstanceLatest(taskView.ProcessInstanceId, nextActivity.ActivityId);
                    if (gatewayActivityInstance != null
                        && !string.IsNullOrEmpty(gatewayActivityInstance.NextStepPerformers))
                    {
                        nextSteps = NextStepUtility.DeserializeNextStepPerformers(gatewayActivityInstance.NextStepPerformers, Sail.JsonSerializer);
                    }
                }
                else if (XPDLHelper.IsInterTimerEventComponentNode(nextActivity) == true)
                {
                    //中间Timer事件节点
                    //Intermediate Timer Event Node
                    var timerActivityInstance = aim.GetActivityInstanceLatest(taskView.ProcessInstanceId, nextActivity.ActivityId);
                    if (timerActivityInstance != null
                        && !string.IsNullOrEmpty(timerActivityInstance.NextStepPerformers))
                    {
                        nextSteps = NextStepUtility.DeserializeNextStepPerformers(timerActivityInstance.NextStepPerformers, Sail.JsonSerializer);
                    }
                }
            }
            return nextSteps;
        }

        /// <summary>
        /// According to the application, obtain the next node list of the process, including role users
        /// 根据应用获取流程下一步节点列表，包含角色用户
        /// </summary>
        /// <param name="resourceService"></param>
        /// <param name="runner"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        internal NextActivityListViewResult GetNextActivityRoleUserTree(IResourceService resourceService, 
            FlowCommandContext runner,
            IDictionary<string, string> condition)
        {
            // 发起前预览只需 ProcessId；运行中查询才需要 AppInstanceId / TaskId
            if (string.IsNullOrEmpty(runner.ProcessId))
            {
                throw new WorkflowException(Sail.Messages.GetEngineMessage("nextstepparser.getnextactivityroleusertree.error"));
            }

            //条件参数一致
            //Consistent condition parameters
            if (condition == null && runner.Conditions != null)
            {
                condition = runner.Conditions;
            }

            NextActivityListViewResult nextTreeResult = null;
            IProcessModel processModel = null;

            using (var kit = Sail.UseRunSQL())
            {
                TaskViewEntity taskView = null;
                var tm = Sail.ManagerFactory.Tasks;
                ProcessInstanceEntity processInstance = null;
                var pim = Sail.ProcessInstances;
                if (runner.TaskId != null)
                {
                    //运行中的流程
                    //Running process
                    taskView = tm.GetTaskView(runner.TaskId);
                    processInstance = pim.GetById(taskView.ProcessInstanceId);
                }
                else if (!string.IsNullOrEmpty(runner.AppInstanceId))
                {
                    var kitForPi = kit.useSQL();
                    var processInstanceList = pim.GetProcessInstance(kitForPi,
                        runner.AppInstanceId,
                        runner.ProcessId,
                        runner.Version).ToList();
                    processInstance = EnumHelper.GetFirst<ProcessInstanceEntity>(processInstanceList);
                }
                // AppInstanceId 空：发起前预览，不查实例

                //判断流程是否创建还是已经运行
                //Determine whether the process has been created or already run
                if (processInstance != null
                    && processInstance.ProcessState == (short)ProcessStateEnum.Running)
                {
                    //运行状态的流程实例
                    //Process instance of running status
                    if (taskView == null)
                    {
                        taskView = tm.GetTaskOfMine(kit.useSQL(), runner);
                    }

                    var isRunningTask = tm.CheckTaskStateInRunningState(taskView);
                    if (isRunningTask == false)
                    {
                        throw new WorkflowException(Sail.Messages.GetEngineMessage("nextstepparser.getnextactivityroleusertree.notrunning.error"));
                    }

                    //读取流程变量表，合并条件参数
                    //Read the process variable table and merge condition parameters
                    var processVariableManager = SailFactoryAccess.Managers(Sail).ProcessVariables;
                    condition = processVariableManager.GetVariableByTask(kit, taskView, condition);

                    //获取下一步列表
                    //Get the next step list
                    processModel = Sail.ProcessModelFactory.CreateByTask(kit.useSQL(), taskView);
                    nextTreeResult = processModel.GetNextActivityListView(taskView.ActivityId, taskView.ActivityInstanceId, condition, kit);
                    
                    foreach (var ns in nextTreeResult.StepList)
                    {
                        //下一步执行人为流程发起人
                        //The next step is for the executor to initiate the process
                        if (ns.ReceiverType == ReceiverTypeEnum.ProcessInitiator)       
                        {
                            ns.Users = AppendUserList(ns.Users, pim.GetProcessInitiator(kit,
                                taskView.ProcessInstanceId));  
                        }
                        else
                        {
                            var roleIDs = ns.Roles != null
                                ? ns.Roles.Select(x => x.Id).ToArray()
                                : Array.Empty<string>();
                            if (roleIDs.Length > 0)
                            {
                                ns.Users = resourceService.GetUserListByRoleReceiverType(roleIDs, runner.UserId, (int)ns.ReceiverType);     //增加转移前置过滤条件
                            }
                        }
                    }
                }
                else
                {
                    // 流程准备启动：从「开始事件」取下一步（首个办理节点），勿从 GetFirstActivity 再往后跳一跳
                    processModel = Sail.ProcessModelFactory.CreateByProcess( runner.ProcessId, runner.Version);
                    var startActivity = processModel?.GetStartActivity();
                    if (startActivity == null)
                    {
                        throw new WorkflowException(
                            Sail.Messages.GetEngineMessage("nextstepparser.getnextactivityroleusertree.error")
                            ?? "start activity not found");
                    }
                    nextTreeResult = processModel.GetNextActivityListView(startActivity.ActivityId,
                        null,
                        condition,
                        kit);
                    if (nextTreeResult?.StepList != null)
                    {
                        foreach (var ns in nextTreeResult.StepList)
                        {
                            var roleIDs = ns.Roles != null ? ns.Roles.Select(x => x.Id).ToArray() : Array.Empty<string>();
                            if (roleIDs.Length > 0)
                                ns.Users = resourceService.GetUserListByRoleReceiverType(roleIDs, runner.UserId, (int)ns.ReceiverType);
                        }
                    }
                }
            }
            return nextTreeResult;
        }

        /// <summary>
        /// Append a single user
        /// 增加单个用户
        /// </summary>
        /// <param name="existUserList"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private IList<User> AppendUserList(IList<User> existUserList, User user)
        {
            if (existUserList == null)
            {
                existUserList = new List<User>();
            }
            existUserList.Add(new User { UserId = user.UserId, UserName = user.UserName });
            return existUserList;
        }


        /// <summary>
        /// Append User List
        /// 添加用户列表
        /// </summary>
        /// <param name="existUserList"></param>
        /// <param name="newUserList"></param>
        /// <returns></returns>
        private IList<User> AppendUserList(IList<User> existUserList, IList<User> newUserList)
        {
            if (existUserList == null)
            {
                existUserList = new List<User>();
            }

            foreach (var user in newUserList)
            {
                if (existUserList.Select(r => r.UserId == user.UserId).ToList() == null)
                {
                    existUserList.Add(new User { UserId = user.UserId, UserName = user.UserName });
                }
            }
            return existUserList;
        }
    }
}
