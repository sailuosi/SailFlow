using System;
using sailflow.engine.Actions;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.Xpdl;
using ActionEntity = sailflow.engine.Xpdl.Action;

namespace sailflow.action.Python
{
    /// <summary>
    /// IronPython Action 方法处理器（ActionMethodEnum.Python）。
    /// </summary>
    public sealed class PythonActionHandler : IActionMethodHandler
    {
        public ActionMethodEnum Method => ActionMethodEnum.Python;

        public void Execute(ActionEntity action, IEventService eventService)
        {
            try
            {
                if (action.CodeInfo != null
                    && !string.IsNullOrEmpty(action.CodeInfo.CodeText))
                {
                    var pythonScript = action.CodeInfo.CodeText;
                    var engine = IronPython.Hosting.Python.CreateEngine();
                    var scope = engine.CreateScope();
                    var dictionary = EventUtil.CompositeKeyValue(action.Arguments, eventService);
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
