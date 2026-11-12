using System;
using sailflow.engine.Actions;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.Xpdl;

namespace sailflow.action.Python
{
    /// <summary>
    /// IronPython Script 方法处理器（ScriptMethodEnum.Python）。
    /// </summary>
    public sealed class PythonScriptHandler : IScriptMethodHandler
    {
        public ScriptMethodEnum Method => ScriptMethodEnum.Python;

        public void Execute(ScriptDetail script, IEventService eventService)
        {
            try
            {
                if (!string.IsNullOrEmpty(script.ScriptText))
                {
                    var pythonScript = script.ScriptText;
                    var engine = IronPython.Hosting.Python.CreateEngine();
                    var scope = engine.CreateScope();
                    var dictionary = EventUtil.CompositeKeyValue(script.Arguments, eventService);
                    foreach (var item in dictionary)
                    {
                        scope.SetVariable(item.Key, item.Value);
                    }
                    var source = engine.CreateScriptSourceFromString(pythonScript);
                    source.Execute(scope);
                }
                else
                {
                    throw new WorkflowException(eventService.Sail.Messages.GetEngineMessage(
                        "actionexecutor.ExecutePythonMethod.warn"));
                }
            }
            catch (Exception ex) when (ex is not WorkflowException)
            {
                throw new WorkflowException(eventService.Sail.Messages.GetEngineMessage(
                    "actionexecutor.ExecutePythonMethod.exception", ex.Message));
            }
        }
    }
}
