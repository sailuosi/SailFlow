using System;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.Xpdl;
using sailflow.WebUtility;

namespace sailflow.engine.Actions
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
