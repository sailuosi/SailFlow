using System;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using IronPython.Hosting;
using saiflow.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Utility;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Event;

namespace saiflow.engine.Core.Pattern.Auto
{
    /// <summary>
    /// Script Executor
    /// ????????
    /// </summary>
    internal class ScriptExecutor
    {
        /// <summary>
        /// Execute script list
        /// </summary>
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

        /// <summary>
        /// Execute
        /// </summary>
        private static void Execute(ScriptDetail script, IEventService eventService)
        {
            if (script.Method == ScriptMethodEnum.SQL)
            {
                ExecuteSQLMethod(script, eventService);
            }
            else if (script.Method == ScriptMethodEnum.Python)
            {
                ExecutePythonMethod(script, eventService);
            }
            else
            {
                throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("actionexecutor.Execute.exception", script.Method.ToString()));
            }
        }


        /// <summary>
        /// Execute Python script
        /// ?????????
        /// SetVariable:
        /// https://stackoverflow.com/questions/26426955/setting-and-getting-variables-in-net-hosted-ironpython-script/45734097
        /// </summary>
        private static void ExecutePythonMethod(ScriptDetail script, IEventService eventService)
        {
            try
            {
                if (!string.IsNullOrEmpty(script.ScriptText))
                {
                    // var pythonScript = action.Expression;
                    var pythonScript = script.ScriptText;         //modified by Besley in 12/26/2019, body is nodetext rather than attribute
                    var engine = Python.CreateEngine();
                    var scope = engine.CreateScope();
                    var dictionary = EventUtil.CompositeKeyValue(script.Arguments, eventService);
                    foreach (var item in dictionary)
                    {
                        scope.SetVariable(item.Key, item.Value);
                    }
                    var source = engine.CreateScriptSourceFromString(pythonScript);
                    source.Execute(scope);
                }
                else
                {
                    throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("actionexecutor.ExecutePythonMethod.warn"));
                }
            }
            catch (Exception ex)
            {
                throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("actionexecutor.ExecutePythonMethod.exception", ex.Message));
            }
        }

        /// <summary>
        /// Excute sql method
        /// </summary>
        private static void ExecuteSQLMethod(ScriptDetail script, IEventService eventService)
        {
            try
            {
                var parameters = EventUtil.CompositeSqlParametersValue(script.Arguments, eventService);
                if (!string.IsNullOrEmpty(script.ScriptText))
                {
                    //var sqlScript = action.Expression;
                    var sqlScript = script.ScriptText;        //modified by Besley in 12/26/2019, body is nodetext rather than attribute
                    var kit = eventService.GetKit();
                    // F2-g1???????? SQL ?? SQLCmd + ?? Executor
                    SailCmdRunner.Execute(kit, sqlScript, parameters);
                }
                else
                {
                    throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("actionexecutor.ExecuteSQLMethod.warn"));
                }
            }
            catch (Exception ex)
            {
                throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("actionexecutor.ExecuteSQLMethod.exception", ex.Message));
            }
        }
    }
}
