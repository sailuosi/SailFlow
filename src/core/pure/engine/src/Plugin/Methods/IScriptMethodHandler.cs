using sailflow.events;
using sailflow.engine.Xpdl;

namespace sailflow.lanes
{
    /// <summary>
    /// BPMN Script 方法处理器（按 <see cref="ScriptMethodEnum"/> 注册）。
    /// </summary>
    public interface IScriptMethodHandler
    {
        ScriptMethodEnum Method { get; }

        void Execute(ScriptDetail script, IEventService eventService);
    }
}
