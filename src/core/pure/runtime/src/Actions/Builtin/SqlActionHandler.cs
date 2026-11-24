using sailflow.data;
using sailflow.lanes;
using sailflow.utils;
using sailflow.events;
using sailflow.xml;
using ActionEntity = sailflow.xml.Action;

namespace sailflow.runtime.Actions;

internal sealed class SqlActionHandler : IActionMethodHandler
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
