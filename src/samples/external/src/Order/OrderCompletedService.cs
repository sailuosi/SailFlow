using System;
using sailflow.engine.Common;
using sailflow.engine.External;

namespace sailflow.hub.External
{
    public class OrderCompletedService : ExternalServiceBase, IExternalService
    {
        public override void Execute()
        {
            //Implement your business logic
            if (EventService != null)
            {
                var id = EventService.GetProcessInstanceId();
                EventService.SaveVariable(ProcessVariableScopeEnum.Process, "price", "1000");
            }
            System.Diagnostics.Debug.WriteLine("order has been completed...");
        }
    }
}
