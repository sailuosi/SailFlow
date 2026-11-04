using System;
using saiflow.data;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Actions
{
    internal sealed class StoreProcedureServiceHandler : IServiceMethodHandler
    {
        public ServiceMethodEnum Method => ServiceMethodEnum.StoreProcedure;

        public void Execute(ServiceDetail service, IEventService eventService)
        {
            try
            {
                var parameters = EventUtil.CompositeSqlParametersValue(service.Arguments, eventService);
                var procedureName = service.Expression;
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
