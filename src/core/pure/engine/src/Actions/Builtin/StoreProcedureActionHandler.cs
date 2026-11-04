using System;
using saiflow.data;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Xpdl;
using ActionEntity = saiflow.engine.Xpdl.Action;

namespace saiflow.engine.Actions
{
    internal sealed class StoreProcedureActionHandler : IActionMethodHandler
    {
        public ActionMethodEnum Method => ActionMethodEnum.StoreProcedure;

        public void Execute(ActionEntity action, IEventService eventService)
        {
            try
            {
                var parameters = EventUtil.CompositeSqlParametersValue(action.Arguments, eventService);
                var procedureName = action.Expression;
                var kit = eventService.GetKit();
                SailCmdRunner.ExecuteProc(kit, procedureName, parameters);
            }
            catch (Exception ex)
            {
                throw new WorkflowException(eventService.Sail.Messages.GetEngineMessage(
                    "actionexecutor.ExecuteStoreProcedureMethod.exception", ex.Message));
            }
        }
    }
}
