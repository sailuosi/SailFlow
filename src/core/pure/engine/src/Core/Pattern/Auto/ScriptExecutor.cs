using System.Collections.Generic;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Event;

namespace saiflow.engine.Core.Pattern.Auto
{
    /// <summary>
    /// Script Executor — 按 <see cref="saiflow.engine.Actions.ScriptMethodRegistry"/> 分发。
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
