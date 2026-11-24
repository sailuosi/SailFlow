using System.Collections.Generic;
using System.Linq;
using sailflow.xml;
using sailflow.events;
using sailflow.run;

namespace sailflow.run{
    /// <summary>
    /// Action Executor — 按 <see cref="sailflow.lanes.ActionMethodRegistry"/> 分发。
    /// </summary>
    internal class ActionExecutor
    {
        internal static void ExecuteActionList(IList<sailflow.xml.Action> actionList,
            IEventService eventService)
        {
            if (actionList != null && actionList.Count > 0)
            {
                foreach (var action in actionList)
                {
                    if (action.FireType != FireTypeEnum.None
                        && (action.ActionMethod != ActionMethodEnum.None))
                    {
                        Execute(action, eventService);
                    }
                }
            }
        }

        internal static void ExecuteActionListBefore(IList<sailflow.xml.Action> actionList,
            IEventService eventService)
        {
            if (actionList != null && actionList.Count > 0)
            {
                var list = actionList.Where(a => a.FireType == FireTypeEnum.Before).ToList();
                if (list != null && list.Count > 0)
                {
                    ExecuteActionList(list, eventService);
                }
            }
        }

        internal static void ExecuteActionListAfter(IList<sailflow.xml.Action> actionList,
            IEventService eventService)
        {
            if (actionList != null && actionList.Count > 0)
            {
                var list = actionList.Where(a => a.FireType == FireTypeEnum.After).ToList();
                if (list != null && list.Count > 0)
                {
                    ExecuteActionList(list, eventService);
                }
            }
        }

        private static void Execute(sailflow.xml.Action action, IEventService eventService)
        {
            if (action.ActionType != ActionTypeEnum.Event)
                return;

            var handler = eventService.Sail.ActionMethods.GetRequired(action.ActionMethod);
            handler.Execute(action, eventService);
        }
    }
}
