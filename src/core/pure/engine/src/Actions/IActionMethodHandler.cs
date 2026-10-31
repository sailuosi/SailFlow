using saiflow.engine.Event;
using saiflow.engine.Xpdl.Common;
using ActionEntity = saiflow.engine.Xpdl.Entity.Action;

namespace saiflow.engine.Actions
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
