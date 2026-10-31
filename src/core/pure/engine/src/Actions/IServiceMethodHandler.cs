using saiflow.engine.Event;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;

namespace saiflow.engine.Actions
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
