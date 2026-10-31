using System.Collections.Generic;
using System.Linq;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Event;

namespace saiflow.engine.Core.Pattern
{
    /// <summary>
    /// Action Executor — 按 <see cref="saiflow.engine.Actions.ActionMethodRegistry"/> 分发。
    /// </summary>
    internal class ActionExecutor
    {
        internal static void ExecuteActionList(IList<Xpdl.Entity.Action> actionList,
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

        internal static void ExecuteActionListBefore(IList<Xpdl.Entity.Action> actionList,
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

        internal static void ExecuteActionListAfter(IList<Xpdl.Entity.Action> actionList,
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

        private static void Execute(Xpdl.Entity.Action action, IEventService eventService)
        {
            if (action.ActionType != ActionTypeEnum.Event)
                return;

            var handler = eventService.Sail.ActionMethods.GetRequired(action.ActionMethod);
            handler.Execute(action, eventService);
        }
    }
}
