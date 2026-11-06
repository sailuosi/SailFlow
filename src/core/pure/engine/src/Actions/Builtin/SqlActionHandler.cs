using System;
using saiflow.data;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Xpdl;
using ActionEntity = saiflow.engine.Xpdl.Action;

namespace saiflow.engine.Actions
{
    public class SqlActionHandler : IActionMethodHandler
    {
        public ActionMethodEnum Method => ActionMethodEnum.SQL;

        public void Execute(ActionEntity action, IEventService eventService)
        {
            try
            {
                var parameters = EventUtil.CompositeSqlParametersValue(action.Arguments, eventService);
                if (action.CodeInfo != null
                    && !string.IsNullOrEmpty(action.CodeInfo.CodeText))
                {
                    var sqlScript = action.CodeInfo.CodeText;
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
