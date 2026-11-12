using System;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.External;
using sailflow.engine.Utility;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Actions
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
