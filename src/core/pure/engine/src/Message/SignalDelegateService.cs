using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using sailflow.engine.Common;
using sailflow.engine.Result;
using sailflow.engine.Service;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;
using sailflow.engine.Manager;
using sailflow.hub.Essential;
using sailflow.WebUtility;

using sailflow.engine.Client;
namespace sailflow.engine.Message
{
    /// <summary>
    /// Signal Delegate Service
    /// 消息队列中的信号委托函数
    /// </summary>
    public class SignalDelegateService
    {
        public SignalDelegateService(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        #region Signal Publish
        /// <summary>
        /// Build Singal Published
        /// 构建要发布的消息
        /// </summary>
        /// <param name="processInstance"></param>
        /// <param name="throwActivity"></param>
        /// <param name="throwActivityInstance"></param>
        public void PublishSignal(ProcessInstanceEntity processInstance,
            Activity throwActivity,
            ActivityInstanceEntity throwActivityInstance)
        {
            var topic = throwActivity.TriggerDetail.Expression;

            var appRunner = new FlowCommandContext();
            appRunner.MessageTopic = topic;
            appRunner.BizChannel = throwActivityInstance.BizChannel;
            appRunner.BizPK = throwActivityInstance.BizPK;
            appRunner.BizCode = throwActivityInstance.BizCode;
            appRunner.UserId = throwActivityInstance.CreatedUserId;
            appRunner.UserName = throwActivityInstance.CreatedUserName;

            var signalRunnerView = new SignalRunnerView();
            signalRunnerView.CommandContext = appRunner;

            var jsonRunner = Sail.JsonSerializer.Serialize(signalRunnerView);

            Sail.GetMessageQueueRequired().Publish(topic, jsonRunner);
        }
        #endregion

        #region Singal Consume
        /// <summary>
        /// Singal Consume function
        /// 消息消费函数
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public List<SignalConsumedResult> ConsumeSignal(SignalEntity entity)
        {
            var resultList = new List<SignalConsumedResult>();
            //get signal runner info
            var signalRunnerView = Sail.JsonSerializer.Deserialize<SignalRunnerView>(entity.Line);
            var appRunner = signalRunnerView.CommandContext;
            var topic = appRunner.MessageTopic;

            var jim = Sail.ManagerFactory.JobInfos;
            var jobInfoList = jim.GetJobSubscribedByTopic(topic);
            foreach (var jobInfo in jobInfoList)
            {
                var signalResult = SignalConsumedResult.Default();
                IWorkflowService wfService = new WorkflowService(Sail);
                WfExecutedResult wfResult = WfExecutedResult.Default();

                var actvityType = EnumHelper.ParseEnum<ActivityTypeEnum>(jobInfo.ActivityType);
                if (actvityType == ActivityTypeEnum.StartNode)
                {
                    var triggerTpe = EnumHelper.ParseEnum<TriggerTypeEnum>(jobInfo.TriggerType);
                    if (triggerTpe == TriggerTypeEnum.Signal)
                    {
                        var direction = EnumHelper.ParseEnum<MessageDirectionEnum>(jobInfo.MessageDirection);
                        if (direction == MessageDirectionEnum.Catch)
                        {
                            var startRunner = GetStartRunnerFromSignalExchange(signalRunnerView, jobInfo);
                            wfResult = wfService.StartProcess(startRunner);
                            if (wfResult.Status == WfExecutedStatus.Success)
                            {
                                signalResult.Status = SignalConsumedStatus.Success;
                            }
                            else
                            {
                                signalResult.Status = SignalConsumedStatus.Exception;
                                signalResult.Message = wfResult.Message;
                            }
                        }
                    }
                    if (signalResult.Status == SignalConsumedStatus.Exception
                        || signalResult.Status == SignalConsumedStatus.Failed)
                    {
                        throw new WfSignalDelegationException(signalResult.Message);
                    }
                    resultList.Add(signalResult);
                }
                else if (actvityType == ActivityTypeEnum.IntermediateNode)
                {
                    var triggerType = EnumHelper.ParseEnum<TriggerTypeEnum>(jobInfo.TriggerType);
                    if (triggerType == TriggerTypeEnum.Signal)
                    {
                        var direction = EnumHelper.ParseEnum<MessageDirectionEnum>(jobInfo.MessageDirection);
                        if (direction == MessageDirectionEnum.Catch)
                        {
                            resultList = RunProcessSignalCatchEventOneByOne(signalRunnerView, jobInfo);
                        }
                    }
                }
            }
            return resultList;
        }

        /// <summary>
        /// Get start info from signal exchange
        /// 从信号交换获取启动信息
        /// </summary>
        /// <param name="msgRunnerView"></param>
        /// <param name="jobInfo"></param>
        /// <returns></returns>
        private FlowCommandContext GetStartRunnerFromSignalExchange(SignalRunnerView msgRunnerView, JobInfoEntity jobInfo)
        {
            var runner = new FlowCommandContext();
            runner.ProcessId = jobInfo.ProcessId;
            runner.Version = jobInfo.Version;
            runner.BizChannel = msgRunnerView.CommandContext.BizChannel;
            runner.BizPK = msgRunnerView.CommandContext.BizPK;
            runner.BizCode = msgRunnerView.CommandContext.BizCode;
            runner.UserId = msgRunnerView.CommandContext.UserId;
            runner.UserName = msgRunnerView.CommandContext.UserName;
            return runner;
        }

        /// <summary>
        /// Run the process belonging to the SignalCatch node
        /// 运行属于SignalCatch节点的流程
        /// </summary>
        /// <param name="signalRunner"></param>
        /// <param name="jobInfo"></param>
        private List<SignalConsumedResult> RunProcessSignalCatchEventOneByOne(SignalRunnerView signalRunner, JobInfoEntity jobInfo)
        {
            var resultList = new List<SignalConsumedResult>();
            //封装下一步的步骤信息
            //Encapsulate the next step information
            var aim = Sail.ManagerFactory.Activities;
            var runningActivityInstanceList = aim.GetActivityInstanceList(jobInfo.ProcessId, jobInfo.Version, jobInfo.ActivityId);
            foreach (var activityInstance in runningActivityInstanceList)
            {
                var signalResult = SignalConsumedResult.Default();
                var runner = GetRunnerFromActivityInstance(signalRunner, jobInfo, activityInstance);
                var wfService = new WorkflowService(Sail);
                var wfResult = wfService.RunProcessAuto(runner);
                if (wfResult.Status == WfExecutedStatus.Success)
                {
                    signalResult.Status = SignalConsumedStatus.Success;
                }
                else
                {
                    signalResult.Status = SignalConsumedStatus.Exception;
                    signalResult.Message = wfResult.Message;
                }

                if (signalResult.Status == SignalConsumedStatus.Exception
                    || signalResult.Status == SignalConsumedStatus.Failed)
                {
                    throw new WfSignalDelegationException(signalResult.Message);
                }
                resultList.Add(signalResult);
            }
            return resultList;
        }

        /// <summary>
        /// Get runner info from activity instance
        /// 从活动实例获取运行者信息
        /// </summary>
        /// <param name="signalRunner"></param>
        /// <param name="jobInfo"></param>
        /// <returns></returns>
        private FlowCommandContext GetRunnerFromActivityInstance(SignalRunnerView signalRunner, 
            JobInfoEntity jobInfo, 
            ActivityInstanceEntity activityInstance)
        {
            var runner = new FlowCommandContext();
            runner.ProcessId = jobInfo.ProcessId;
            runner.Version = jobInfo.Version;
            runner.BizChannel = activityInstance.BizChannel;
            runner.BizPK = activityInstance.BizPK;
            runner.BizCode = activityInstance.BizCode;
            //using activityinstanceid to call RunProcessAuto() method
            runner.ActivityInstanceId = activityInstance.ActivityInstanceId;
            runner.UserId = signalRunner.CommandContext.UserId;
            runner.UserName = signalRunner.CommandContext.UserName;

            if (!string.IsNullOrEmpty(activityInstance.NextStepPerformers))
            {
                runner.NextActivityPerformers = NextStepUtility.DeserializeNextStepPerformers(
                    activityInstance.NextStepPerformers, Sail.JsonSerializer);
            }
            return runner;
        }
        #endregion
    }
}
