using System;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.Xpdl;
using sailflow.WebUtility;
using ActionEntity = sailflow.engine.Xpdl.Action;

namespace sailflow.engine.Actions
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
