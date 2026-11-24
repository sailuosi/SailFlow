using sailflow.data;
using sailflow.engine.Actions;
using sailflow.engine.Common;
using sailflow.events;
using sailflow.engine.Xpdl;
using ActionEntity = sailflow.engine.Xpdl.Action;

namespace sailflow.runtime.Actions;

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
