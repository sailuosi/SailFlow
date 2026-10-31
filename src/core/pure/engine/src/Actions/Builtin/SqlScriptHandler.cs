using System;
using saiflow.data;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;

namespace saiflow.engine.Actions
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
