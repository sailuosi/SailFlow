using System;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.External;
using saiflow.engine.Utility;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Actions
{
    internal sealed class LocalServiceServiceHandler : IServiceMethodHandler
    {
        public ServiceMethodEnum Method => ServiceMethodEnum.LocalService;

        public void Execute(ServiceDetail service, IEventService eventService)
        {
            try
            {
                var instance = ReflectionHelper.GetSpecialInstance<IExternalService>(service.Expression);
                var executableInstance = instance as IExecutable;
                executableInstance.Executable(eventService);
            }
            catch (Exception ex)
            {
                throw new WorkflowException(eventService.Sail.Messages.GetEngineMessage(
                    "actionexecutor.ExecuteLocalService.exception", ex.Message));
            }
        }
    }
}
