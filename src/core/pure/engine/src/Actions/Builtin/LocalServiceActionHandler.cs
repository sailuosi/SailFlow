using System;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.External;
using sailflow.engine.Utility;
using sailflow.engine.Xpdl;
using ActionEntity = sailflow.engine.Xpdl.Action;

namespace sailflow.engine.Actions
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
