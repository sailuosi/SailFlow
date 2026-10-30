using System;
using saiflow.data;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Client;
using saiflow.engine.Event.Handlers;

namespace saiflow.engine.Event
{
    /// <summary>
    /// Event Service Factory
    /// </summary>
    public class EventServiceFactory
    {
        public static EventServiceBase CreateEventService(EventScopeTypeEnum scopeType,
            SQLBuilder kit,
            EventContext context,
            SailClient client)
        {
            if (scopeType == EventScopeTypeEnum.Process)
            {
                return new ProcessEventService(kit, context, client);
            }
            else if (scopeType == EventScopeTypeEnum.Activity)
            {
                return new ActivityEventService(kit, context, client);
            }
            else
            {
                throw new ApplicationException(LocalizeHelper.GetEngineMessage("eventservicefactory.CreateEventService.error"));
            }
        }
    }
}
