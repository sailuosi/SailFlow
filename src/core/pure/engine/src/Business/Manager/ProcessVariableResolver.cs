using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Client;
using saiflow.engine.Xpdl.Entity;

namespace saiflow.engine.Business.Manager
{
    internal class ProcessVariableResolver
    {
        public ProcessVariableResolver(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        /// <summary>检查必填输出变量是否存在（有事务时用上游 kit）。</summary>
        internal bool CheckVariableRequired(
            SQLBuilder kit,
            TaskViewEntity taskView,
            Activity activity,
            IDictionary<string, string> conditions)
        {
            var isCheckPassed = true;
            var variableList = activity.VariableList;
            if (variableList == null)
                return isCheckPassed;

            var outputVariableList = variableList
                .Where(v => v.DirectionType == VariableDirectionTypeEnum.Output)
                .ToList();
            var pvm = new ProcessVariableManager(Sail);
            foreach (var ov in outputVariableList)
            {
                if (ov.IsRequired != true)
                    continue;

                var ovDbEntity = pvm.GetVariableByActivity(
                    kit, taskView.ProcessInstanceId.Value, taskView.ActivityInstanceId.Value, ov.Name);
                if (ovDbEntity == null)
                {
                    isCheckPassed = false;
                    break;
                }

                if (!conditions.ContainsKey(ovDbEntity.Name))
                    conditions.Add(ovDbEntity.Name, ovDbEntity.Value);
            }
            return isCheckPassed;
        }
    }
}
