using System;
using System.Collections.Generic;
using System.Linq;
using saiflow.data;
using saiflow.hub.Localize;
using saiflow.hub.Resource;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.WebUtility;

using saiflow.engine.Client;
namespace saiflow.engine.Core.Parser
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
            WfAppRunner runner, 
            IDictionary<string, string> condition = null)
        {
            NextStepInfo nextStepInfo = new NextStepInfo();
            var nextResult = GetNextActivityRoleUserTree(resourceService, runner, condition);
            nextStepInfo.Message = nextResult.Message;
            nextStepInfo.NextActivityRoleUserTree = nextResult.StepList;
            if (nextResult.StepList != null && nextResult.StepList.Count > 0)
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
        private IDictionary<string, PerformerList> GetNextActivityPerformersPriliminary(WfAppRunner runner)
        {
            IDictionary<string, PerformerList> nextSteps = null;

            var tm = new TaskManager(Sail);
            TaskViewEntity taskView = tm.GetTaskOfMine(runner);

            //读取活动实例中记录的步骤预选数据
            //Read the pre selected data of the steps recorded in the activity instance
            var aim = new ActivityInstanceManager(Sail);
            if (taskView.MainActivityInstanceId != null)
            {
                var mainActivityInstanceId = taskView.MainActivityInstanceId.Value;
                var mainActivityInstance = aim.GetById(mainActivityInstanceId);
                if (mainActivityInstance != null)
                {
                    nextSteps = NextStepUtility.DeserializeNextStepPerformers(mainActivityInstance.NextStepPerformers);
                }
            }

            //获取下一步信息
            //Obtain the next step information
            var processModel = ProcessModelFactory.CreateByTask(Sail, taskView);
            var nextActivity = processModel.GetNextActivity(taskView.ActivityId);
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
                        nextSteps = NextStepUtility.DeserializeNextStepPerformers(gatewayActivityInstance.NextStepPerformers);
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
                        nextSteps = NextStepUtility.DeserializeNextStepPerformers(timerActivityInstance.NextStepPerformers);
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
            WfAppRunner runner,
            IDictionary<string, string> condition)
        {
            //判断应用数据是否缺失
            //Determine whether the application data is missing
            if (string.IsNullOrEmpty(runner.AppInstanceId)
                || string.IsNullOrEmpty(runner.ProcessId))
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
                var tm = new TaskManager(Sail);
                ProcessInstanceEntity processInstance = null;
                var pim = new ProcessInstanceManager(Sail);
                if (runner.TaskId != null)
                {
                    //运行中的流程
                    //Running process
                    taskView = tm.GetTaskView(runner.TaskId.Value);
                    processInstance = pim.GetById(taskView.ProcessInstanceId);
                }
                else
                {
                    var kitForPi = SailSqlAccess.Sibling(kit);
                    var processInstanceList = pim.GetProcessInstance(kitForPi,
                        runner.AppInstanceId,
                        runner.ProcessId,
                        runner.Version).ToList();
                    processInstance = EnumHelper.GetFirst<ProcessInstanceEntity>(processInstanceList);
                }

                //判断流程是否创建还是已经运行
                //Determine whether the process has been created or already run
                if (processInstance != null
                    && processInstance.ProcessState == (short)ProcessStateEnum.Running)
                {
                    //运行状态的流程实例
                    //Process instance of running status
                    if (taskView == null)
                    {
                        taskView = tm.GetTaskOfMine(SailSqlAccess.Sibling(kit), runner);
                    }

                    var isRunningTask = tm.CheckTaskStateInRunningState(taskView);
                    if (isRunningTask == false)
                    {
                        throw new WorkflowException(Sail.Messages.GetEngineMessage("nextstepparser.getnextactivityroleusertree.notrunning.error"));
                    }

                    //读取流程变量表，合并条件参数
                    //Read the process variable table and merge condition parameters
                    var processVariableManager = new ProcessVariableManager(Sail);
                    condition = processVariableManager.GetVariableByTask(kit, taskView, condition);

                    //获取下一步列表
                    //Get the next step list
                    processModel = ProcessModelFactory.CreateByTask(Sail, SailSqlAccess.Sibling(kit), taskView);
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
                            var roleIDs = ns.Roles.Select(x => x.Id).ToArray();
                            if (roleIDs.Count() > 0)
                            {
                                ns.Users = resourceService.GetUserListByRoleReceiverType(roleIDs, runner.UserId, (int)ns.ReceiverType);     //增加转移前置过滤条件
                            }
                        }
                    }
                }
                else
                {
                    //读取流程变量表，合并条件参数
                    //Read the process variable table and merge condition parameters

                    //流程准备启动，获取第一个办理节点的用户列表
                    //Process preparation to start, obtain the user list of the first processing node
                    processModel = ProcessModelFactory.CreateByProcess(Sail, runner.ProcessId, runner.Version);
                    var firstActivity = processModel.GetFirstActivity();
                    nextTreeResult = processModel.GetNextActivityListView(firstActivity.ActivityId,
                        null,
                        condition,
                        kit);
                    foreach (var ns in nextTreeResult.StepList)
                    {
                        var roleIDs = ns.Roles.Select(x => x.Id).ToArray();
                        ns.Users = resourceService.GetUserListByRoleReceiverType(roleIDs, runner.UserId, (int)ns.ReceiverType);     //增加转移前置过滤条件
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
