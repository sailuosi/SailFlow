using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.engine.Manager;
using saiflow.engine.Client;
using saiflow.engine.Core;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Event
{
    /// <summary>
    /// Event Executor
    /// 事件执行器
    /// </summary>
    internal class EventExecutor
    {
        /// <summary>
        /// 当前 <see cref="Execute"/> 会真正 Invoke 订阅回调的类型：
        /// 流程级 OnProcess*（含 Starting/Started/Running/Continued/Completed 及 Withdraw/SendBack/Resend/Reverse/Jump/SignForward/Revise/Reject/Close 等 *ing/*ed）、
        /// 活动级 OnActivity*、任务级 OnTaskCreated/OnTaskCompleted。
        /// 其它枚举值（OnSuccess/OnError/OnWarning/None）不在此路径执行。
        /// </summary>
        private static readonly HashSet<EventFireTypeEnum> ProcessScopeEvents = new()
        {
            EventFireTypeEnum.OnProcessStarting,
            EventFireTypeEnum.OnProcessStarted,
            EventFireTypeEnum.OnProcessRunning,
            EventFireTypeEnum.OnProcessContinued,
            EventFireTypeEnum.OnProcessCompleted,
            EventFireTypeEnum.OnProcessWithdrawing,
            EventFireTypeEnum.OnProcessWithdrawn,
            EventFireTypeEnum.OnProcessSendBacking,
            EventFireTypeEnum.OnProcessSendBacked,
            EventFireTypeEnum.OnProcessResending,
            EventFireTypeEnum.OnProcessResent,
            EventFireTypeEnum.OnProcessReversing,
            EventFireTypeEnum.OnProcessReversed,
            EventFireTypeEnum.OnProcessJumping,
            EventFireTypeEnum.OnProcessJumped,
            EventFireTypeEnum.OnProcessSignForwarding,
            EventFireTypeEnum.OnProcessSignForwarded,
            EventFireTypeEnum.OnProcessRevising,
            EventFireTypeEnum.OnProcessRevised,
            EventFireTypeEnum.OnProcessRejecting,
            EventFireTypeEnum.OnProcessRejected,
            EventFireTypeEnum.OnProcessClosing,
            EventFireTypeEnum.OnProcessClosed,
        };

        private static readonly HashSet<EventFireTypeEnum> ActivityScopeEvents = new()
        {
            EventFireTypeEnum.OnActivityCreated,
            EventFireTypeEnum.OnActivityExecuting,
            EventFireTypeEnum.OnActivityExecuted,
            EventFireTypeEnum.OnActivityCompleted,
            EventFireTypeEnum.OnTaskCreated,
            EventFireTypeEnum.OnTaskCompleted,
        };

        public EventExecutor(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        internal static void InvokeExternalEvent(SailClient client, SQLBuilder kit,
            EventFireTypeEnum eventType,
            Activity currentActivity,
            ActivityForwardContext context)
        {
            var eventContext = new EventContext
            {
                AppInstanceId = context.ProcessInstance.AppInstanceId,
                ProcessId = context.ProcessInstance.ProcessId,
                ProcessInstanceId = context.ProcessInstance.Id.Value,
                ActivityId = currentActivity.ActivityId,
                ActivityName = currentActivity.ActivityName,
                ActivityCode = currentActivity.ActivityCode,
                ActivityResource = context.ActivityResource
            };
            InvokeExternalEvent(client, kit, eventType,
                context.ActivityResource.AppRunner.EventSubscriptionList,
                    eventContext);
        }

        internal static void InvokeExternalEvent(SailClient client, SQLBuilder kit,
            EventFireTypeEnum eventType,
            EventSubscriptionList eventList,
            int processInstanceId)
        {
            var eventListFiltered = eventList.Where(k => k.Key == eventType);
            if (eventListFiltered != null)
            {
                var pim = new ProcessInstanceManager(client);
                var entity = pim.GetById(kit, processInstanceId);

                var context = new EventContext
                {
                    AppInstanceId = entity.AppInstanceId,
                    ProcessId = entity.ProcessId,
                    ProcessInstanceId = processInstanceId
                };
                Execute(client, kit, context, eventListFiltered);
            }
        }

        internal static void InvokeExternalEvent(SailClient client, SQLBuilder kit,
            EventFireTypeEnum eventType,
            EventSubscriptionList eventList,
            EventContext context)
        {
            var eventListFiltered = eventList.Where(k => k.Key == eventType);

            if (eventListFiltered != null)
            {
                Execute(client, kit, context, eventListFiltered);
            }
        }

        private static void Execute(SailClient client, SQLBuilder kit,
            EventContext context,
            IEnumerable<KeyValuePair<EventFireTypeEnum, Func<EventContext, IEventService, Boolean>>> eventList)
        {
            foreach (var e in eventList)
            {
                Execute(client, kit, context, e);
            }
        }

        private static object Execute(SailClient client, SQLBuilder kit,
            EventContext context,
            KeyValuePair<EventFireTypeEnum, Func<EventContext, IEventService, Boolean>> item)
        {
            if (ProcessScopeEvents.Contains(item.Key))
            {
                var eventService = EventServiceFactory.CreateEventService(EventScopeTypeEnum.Process, kit, context, client);
                eventService.SetActivityResource(context.ActivityResource);
                return item.Value(context, eventService as IEventService);
            }

            if (ActivityScopeEvents.Contains(item.Key))
            {
                var eventService = EventServiceFactory.CreateEventService(EventScopeTypeEnum.Activity, kit, context, client);
                eventService.SetActivityResource(context.ActivityResource);
                return item.Value(context, eventService as IEventService);
            }

            return false;
        }
    }
}
