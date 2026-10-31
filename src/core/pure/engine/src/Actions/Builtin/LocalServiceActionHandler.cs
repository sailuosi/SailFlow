using System;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.External;
using saiflow.engine.Utility;
using saiflow.engine.Xpdl.Common;
using ActionEntity = saiflow.engine.Xpdl.Entity.Action;

namespace saiflow.engine.Actions
{
    internal sealed class LocalServiceActionHandler : IActionMethodHandler
    {
        public ActionMethodEnum Method => ActionMethodEnum.LocalService;

        public void Execute(ActionEntity action, IEventService eventService)
        {
            try
            {
                var instance = ReflectionHelper.GetSpecialInstance<IExternalService>(action.Expression);
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
