using System;
using sailflow.data;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Actions
{
    internal sealed class SqlScriptHandler : IScriptMethodHandler
    {
        public ScriptMethodEnum Method => ScriptMethodEnum.SQL;

        public void Execute(ScriptDetail script, IEventService eventService)
        {
            try
            {
                var parameters = EventUtil.CompositeSqlParametersValue(script.Arguments, eventService);
                if (!string.IsNullOrEmpty(script.ScriptText))
                {
                    var sqlScript = script.ScriptText;
                    var kit = eventService.GetKit();
                    SailCmdRunner.Execute(kit, sqlScript, parameters);
                }
                else
                {
                    throw new WorkflowException(eventService.Sail.Messages.GetEngineMessage(
                        "actionexecutor.ExecuteSQLMethod.warn"));
                }
            }
            catch (Exception ex) when (ex is not WorkflowException)
            {
                throw new WorkflowException(eventService.Sail.Messages.GetEngineMessage(
                    "actionexecutor.ExecuteSQLMethod.exception", ex.Message));
            }
        }
    }
}
