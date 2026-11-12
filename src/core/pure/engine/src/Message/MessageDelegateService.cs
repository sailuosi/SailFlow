using System;
using System.Collections.Generic;
using System.Text;
using sailflow.engine.Common;
using sailflow.engine.Result;
using sailflow.engine.Service;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;
using sailflow.engine.Manager;
using sailflow.hub.Essential;

using sailflow.engine.Client;
namespace sailflow.engine.Message
{
    /// <summary>
    /// Message delegation function in the message queue
    /// 消息队列中的消息委托函数
    /// </summary>
    public class MessageDelegateService
    {
        public MessageDelegateService(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        #region Message Publish
        /// <summary>
        /// Build the message to be published
        /// 构建要发布的消息
        /// </summary>
        /// <param name="processInstance"></param>
        /// <param name="throwActivity"></param>
        /// <param name="throwActivityInstance"></param>
        public void PublishMessage(ProcessInstanceEntity processInstance,
            Activity throwActivity,
            ActivityInstanceEntity throwActivityInstance)
        {
            var topic = throwActivity.TriggerDetail.Expression;
            ProcessEntity catchProcess = null;
            var catchActivity = XPDLHelper.GetMessageCatchActivity(Sail, processInstance, throwActivity, throwActivityInstance, out catchProcess);

            var appRunner = new FlowCommandContext();
            appRunner.MessageTopic = topic;
            appRunner.AppName = throwActivityInstance.AppName;
            appRunner.AppInstanceId = throwActivityInstance.AppInstanceId;
            appRunner.AppInstanceCode = throwActivityInstance.AppInstanceCode;
            appRunner.UserId = throwActivityInstance.CreatedUserId;
            appRunner.UserName = throwActivityInstance.CreatedUserName;

            var msgRunnerView = new MessageRunnerView();
            msgRunnerView.ProcessEntity = catchProcess;
            msgRunnerView.ActivityEntity = catchActivity;
            msgRunnerView.CommandContext = appRunner;

            var jsonRunner = Sail.JsonSerializer.Serialize(msgRunnerView);

            Sail.GetMessageQueueRequired().Publish(topic, jsonRunner);
        }
        #endregion

        #region Message Consume
        /// <summary>
        /// Message consumption function
        /// 消息消费函数
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public MessageConsumedResult ConsumeMessage(MessageEntity entity)
        {
            var msgResult = MessageConsumedResult.Default();
            var topic = entity.Topic;
            var msgRunnerView = Sail.JsonSerializer.Deserialize<MessageRunnerView>(entity.Line);

            if (!string.IsNullOrEmpty(topic))
            {
                try
                {
                    IWorkflowService wfService = new WorkflowService(Sail);
                    WfExecutedResult wfResult = WfExecutedResult.Default();
                    var catchActivity = msgRunnerView.ActivityEntity;
                    if (catchActivity.ActivityType == ActivityTypeEnum.StartNode)
                    {
                        if (catchActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Message
                            && catchActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                        {
                            var startRunner = GetRunnerFromMessagExchange(msgRunnerView);
                            wfResult = wfService.StartProcess(startRunner);
                            if (wfResult.Status == WfExecutedStatus.Success)
                            {
                                msgResult.Status = MessageConsumedStatus.Success;
                            }
                            else
                            {
                                msgResult.Status = MessageConsumedStatus.Exception;
                                msgResult.Message = wfResult.Message;
                            }
                        }
                        else
                        {
                            msgResult.Status = MessageConsumedStatus.Exception;
                            msgResult.Message = string.Format("Unknown activity type detail:{0}, message direction:{1}",
                                catchActivity.TriggerDetail.TriggerType.ToString(),
                                catchActivity.TriggerDetail.MessageDirection.ToString());
                        }
                    }
                    else
                    {
                        if (catchActivity.TriggerDetail.TriggerType == TriggerTypeEnum.Message
                            && catchActivity.TriggerDetail.MessageDirection == MessageDirectionEnum.Catch)
                        {
                            var runner = GetRunnerFromMessagExchange(msgRunnerView);
                            wfResult = wfService.RunProcessAuto(runner);
                            if (wfResult.Status == WfExecutedStatus.Success)
                            {
                                msgResult.Status = MessageConsumedStatus.Success;
                            }
                            else
                            {
                                msgResult.Status = MessageConsumedStatus.Exception;
                                msgResult.Message = wfResult.Message;
                            }
                        }
                        else
                        {
                            msgResult.Status = MessageConsumedStatus.Exception;
                            msgResult.Message = string.Format("Unknown activity type detail:{0}, message direction:{1}",
                                catchActivity.TriggerDetail.TriggerType.ToString(),
                                catchActivity.TriggerDetail.MessageDirection.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    msgResult.Status = MessageConsumedStatus.Failed;
                    msgResult.Message = ex.Message;
                    throw;
                }
            }

            if (msgResult.Status == MessageConsumedStatus.Exception
                || msgResult.Status == MessageConsumedStatus.Failed)
            {
                throw new WfMessageDelegationException(msgResult.Message);
            }
            return msgResult;
        }

        /// <summary>
        /// Encapsulate the operator object from the message view
        /// 从消息视图中封装运行者对象
        /// </summary>
        /// <param name="msgRunnerView"></param>
        /// <returns></returns>
        private FlowCommandContext GetRunnerFromMessagExchange(MessageRunnerView msgRunnerView)
        {
            var runner = new FlowCommandContext();
            runner.ProcessId = msgRunnerView.ProcessEntity.ProcessId;
            runner.Version = msgRunnerView.ProcessEntity.Version;
            runner.ProcessCode = msgRunnerView.ProcessEntity.ProcessCode;
            runner.AppName = msgRunnerView.CommandContext.AppName;
            runner.AppInstanceId = msgRunnerView.CommandContext.AppInstanceId;
            runner.AppInstanceCode = msgRunnerView.CommandContext.AppInstanceCode;
            runner.UserId = msgRunnerView.CommandContext.UserId;
            runner.UserName = msgRunnerView.CommandContext.UserName;

            //封装下一步的步骤信息
            //Encapsulate the next step information
            var aim = Sail.ManagerFactory.Activities;
            var runningActivityInstance = aim.GetActivityInstanceLatest(runner.AppInstanceId, runner.ProcessId, msgRunnerView.ActivityEntity.ActivityId);
            if (runningActivityInstance != null)
            {
                runner.ActivityInstanceId = runningActivityInstance.ActivityInstanceId;
                if (!string.IsNullOrEmpty(runningActivityInstance.NextStepPerformers))
                {
                    runner.NextActivityPerformers = NextStepUtility.DeserializeNextStepPerformers(
                        runningActivityInstance.NextStepPerformers, Sail.JsonSerializer);
                }
            }

            return runner;
        }
        #endregion
    }
}
