using sailflow.events;
using sailflow.xml;
using ActionEntity = sailflow.xml.Action;

namespace sailflow.lanes
{
    /// <summary>
    /// BPMN Action 方法处理器（按 <see cref="ActionMethodEnum"/> 注册）。
    /// </summary>
    public interface IActionMethodHandler
    {
        ActionMethodEnum Method { get; }

        void Execute(ActionEntity action, IEventService eventService);
    }
}
