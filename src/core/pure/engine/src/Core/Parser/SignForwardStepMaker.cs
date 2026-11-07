using System;
using System.Collections.Generic;
using System.Linq;
using saiflow.data;
using saiflow.hub.Localize;
using saiflow.hub.Resource;
using saiflow.engine.Common;
using saiflow.engine.Utility;
using saiflow.engine.Xpdl;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using System.Reflection;

using saiflow.engine.Client;
using saiflow.engine.Core;
namespace saiflow.engine.Runtime
{
    /// <summary>
    /// Sign Forward Step Maker
    /// 加签步骤生成器
    /// </summary>
    internal class SignForwardStepMaker
    {
        public SignForwardStepMaker(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        /// <summary>
        /// Obtain information on the steps for adding a signature
        /// 获取加签步骤信息
        /// </summary>
        /// <param name="runner"></param>
        /// <returns></returns>
        internal SignForwardStepInfo GetSignForwardStepInfo(FlowCommandContext runner)
        {
            var signForwardStepInfo = new SignForwardStepInfo();
            var signResult = GetSignForwardRoleUserTree(runner);
            signForwardStepInfo.SignForwardRoleUserTree = signResult.StepList;
            return signForwardStepInfo;
        }

        /// <summary>
        /// According to the application, obtain the next node list of the process, including role users
        /// 根据应用获取流程下一步节点列表，包含角色用户
        /// </summary>
        /// <param name="runner"></param>
        /// <returns></returns>
        internal NextActivityListViewResult GetSignForwardRoleUserTree(FlowCommandContext runner)
        {
            var nextTreeResult = new NextActivityListViewResult();

            //判断应用数据是否缺失
            //Determine whether the application data is missing
            if (string.IsNullOrEmpty(runner.AppInstanceId)
                || string.IsNullOrEmpty(runner.ProcessId))
            {
                throw new WorkflowException(Sail.Messages.GetEngineMessage("nextstepparser.getnextactivityroleusertree.error"));
            }

            using (var kit = Sail.UseRunSQL())
            {
                //运行状态的流程实例
                //Process instance of running status
                var tm = Sail.ManagerFactory.Tasks;
                TaskViewEntity taskView = tm.GetTaskOfMine(kit.useSQL(), runner);

                var isRunningTask = tm.CheckTaskStateInRunningState(taskView);
                if (isRunningTask == false)
                {
                    throw new WorkflowException(Sail.Messages.GetEngineMessage("nextstepparser.getnextactivityroleusertree.notrunning.error"));
                }

                var treeNodeList = new List<NodeView>();
                var processModel = Sail.ProcessModelFactory.CreateByTask(kit.useSQL(), taskView);
                var currentActivity = processModel.GetActivity(taskView.ActivityId);
                treeNodeList.Add(new NodeView
                {
                    ActivityId = currentActivity.ActivityId,
                    ActivityName = currentActivity.ActivityName,
                    ActivityCode = currentActivity.ActivityCode,
                    ActivityUrl = currentActivity.ActivityUrl,
                    MyProperties = currentActivity.MyProperties,
                    ActivityType = currentActivity.ActivityType,
                    Roles = processModel.GetActivityRoles(currentActivity.ActivityId)
                });
                nextTreeResult.StepList = treeNodeList;
            }
            return nextTreeResult;
        }
     }
}
