using System.Collections.Generic;
using sailflow.xml;
using sailflow.events;
using sailflow.run;

namespace sailflow.run{
    /// <summary>
    /// Service Executor — 按 <see cref="sailflow.lanes.ServiceMethodRegistry"/> 分发。
    /// </summary>
    internal class ServiceExecutor
    {
        internal static void ExecuteServiceList(IList<ServiceDetail> serviceList,
            ActivityForwardContext forwardContext,
            IEventService eventService)
        {
            if (serviceList != null && serviceList.Count > 0)
            {
                foreach (var service in serviceList)
                {
                    if (service.Method != ServiceMethodEnum.None)
                    {
                        Execute(service, forwardContext, eventService);
                    }
                }
            }
        }

        private static void Execute(ServiceDetail service,
            ActivityForwardContext forwardContext,
            IEventService eventService)
        {
            var handler = eventService.Sail.ServiceMethods.GetRequired(service.Method);
            handler.Execute(service, eventService);
        }
    }
}
