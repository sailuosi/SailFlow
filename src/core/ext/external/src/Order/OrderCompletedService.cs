using System;
using saiflow.engine.Common;
using saiflow.engine.External;

namespace saiflow.hub.External.Order
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
