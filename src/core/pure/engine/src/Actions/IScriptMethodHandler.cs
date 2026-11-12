using sailflow.engine.Event;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Actions
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
