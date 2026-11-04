using System;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Xpdl;
using saiflow.WebUtility;
using ActionEntity = saiflow.engine.Xpdl.Action;

namespace saiflow.engine.Actions
{
    internal sealed class WebApiActionHandler : IActionMethodHandler
    {
        public ActionMethodEnum Method => ActionMethodEnum.WebApi;

        public void Execute(ActionEntity action, IEventService eventService)
        {
            try
            {
                if (action.SubMethod == SubMethodEnum.HttpGet)
                {
                    var jsonGetValue = eventService.GetVariableByScopePriority(action.Arguments);
                    var url = string.Format("{0}/{1}", action.Expression, jsonGetValue);
                    HttpClientHelper.CreateHelper(url).Get();
                }
                else if (action.SubMethod == SubMethodEnum.HttpPost)
                {
                    var httpClientHelper = HttpClientHelper.CreateHelper(action.Expression);
                    var jsonValue = EventUtil.CompositeJsonValue(action.Arguments, eventService);
                    httpClientHelper.Post(jsonValue);
                }
                else if (action.SubMethod == SubMethodEnum.HttpPut)
                {
                    var httpClientHelper = HttpClientHelper.CreateHelper(action.Expression);
                    var jsonValue = EventUtil.CompositeJsonValue(action.Arguments, eventService);
                    httpClientHelper.Put(jsonValue);
                }
                else if (action.SubMethod == SubMethodEnum.HttpDelete)
                {
                    var jsonGetValue = eventService.GetVariableByScopePriority(action.Arguments);
                    var url = string.Format("{0}/{1}", action.Expression, jsonGetValue);
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
