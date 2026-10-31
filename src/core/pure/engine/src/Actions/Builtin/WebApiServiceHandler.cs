using System;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.WebUtility;

namespace saiflow.engine.Actions
{
    internal sealed class WebApiServiceHandler : IServiceMethodHandler
    {
        public ServiceMethodEnum Method => ServiceMethodEnum.WebApi;

        public void Execute(ServiceDetail service, IEventService eventService)
        {
            try
            {
                if (service.SubMethod == SubMethodEnum.HttpGet)
                {
                    var jsonGetValue = eventService.GetVariableByScopePriority(service.Arguments);
                    var url = string.Format("{0}/{1}", service.Expression, jsonGetValue);
                    HttpClientHelper.CreateHelper(url).Get();
                }
                else if (service.SubMethod == SubMethodEnum.HttpPost)
                {
                    var httpClientHelper = HttpClientHelper.CreateHelper(service.Expression);
                    var jsonValue = EventUtil.CompositeJsonValue(service.Arguments, eventService);
                    httpClientHelper.Post(jsonValue);
                }
                else if (service.SubMethod == SubMethodEnum.HttpPut)
                {
                    var httpClientHelper = HttpClientHelper.CreateHelper(service.Expression);
                    var jsonValue = EventUtil.CompositeJsonValue(service.Arguments, eventService);
                    httpClientHelper.Put(jsonValue);
                }
                else if (service.SubMethod == SubMethodEnum.HttpDelete)
                {
                    var jsonGetValue = eventService.GetVariableByScopePriority(service.Arguments);
                    var url = string.Format("{0}/{1}", service.Expression, jsonGetValue);
                    HttpClientHelper.CreateHelper(url).Delete();
                }
            }
            catch (Exception ex)
            {
                throw new WorkflowException(eventService.Sail.Messages.GetEngineMessage(
                    "actionexecutor.ExecuteWebApi.exception", ex.Message));
            }
        }
    }
}
