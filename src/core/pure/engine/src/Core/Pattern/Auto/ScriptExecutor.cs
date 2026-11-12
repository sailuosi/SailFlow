using System.Collections.Generic;
using sailflow.engine.Xpdl;
using sailflow.engine.Event;
using sailflow.engine.Core;

namespace sailflow.engine.Pattern
{
    /// <summary>
    /// Script Executor — 按 <see cref="sailflow.engine.Actions.ScriptMethodRegistry"/> 分发。
    /// </summary>
    internal class ScriptExecutor
    {
        internal static void ExecuteScriptList(IList<ScriptDetail> scriptList,
            IEventService eventService)
        {
            if (scriptList != null && scriptList.Count > 0)
            {
                foreach (var script in scriptList)
                {
                    if (script.Method != ScriptMethodEnum.None)
                    {
                        Execute(script, eventService);
                    }
                }
            }
        }

        private static void Execute(ScriptDetail script, IEventService eventService)
        {
            var handler = eventService.Sail.ScriptMethods.GetRequired(script.Method);
            handler.Execute(script, eventService);
        }
    }
}
