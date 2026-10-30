using System;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Event.Handlers;

namespace saiflow.engine.Event
{
    /// <summary>
    /// Event Service Factory
    /// 事件服务创建类
    /// </summary>
    public class EventServiceFactory
    {
        /// <summary>
        /// Create Event Service
        /// 创建事件服务
        /// </summary>
        public static EventServiceBase CreateEventService(EventScopeTypeEnum scopeType, 
            SQLBuilder kit, 
            EventContext context)
        {
            if (scopeType == EventScopeTypeEnum.Process)
            {
                var processEventService = new ProcessEventService(kit, context);
                return processEventService;
            }
            else if (scopeType == EventScopeTypeEnum.Activity)
            {
                var activityEventService = new ActivityEventService(kit, context);
                return activityEventService;
            }
            else
            {
                throw new ApplicationException(LocalizeHelper.GetEngineMessage("eventservicefactory.CreateEventService.error"));
            }
        }
    }
}
