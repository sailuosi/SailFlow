using sailflow.engine.Event;
using sailflow.engine.Xpdl;
using ActionEntity = sailflow.engine.Xpdl.Action;

namespace sailflow.engine.Actions
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
