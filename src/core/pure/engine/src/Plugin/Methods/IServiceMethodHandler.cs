using sailflow.events;
using sailflow.xml;

namespace sailflow.lanes
{
    /// <summary>
    /// BPMN Service 方法处理器（按 <see cref="ServiceMethodEnum"/> 注册）。
    /// </summary>
    public interface IServiceMethodHandler
    {
        ServiceMethodEnum Method { get; }

        void Execute(ServiceDetail service, IEventService eventService);
    }
}
