using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Client;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Manager
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

            var requiredNames = variableList
                .Where(v => v.DirectionType == VariableDirectionTypeEnum.Output && v.IsRequired == true)
                .Select(v => v.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (requiredNames.Count == 0)
                return isCheckPassed;

            var pvm = new ProcessVariableManager(Sail);
            var loaded = pvm.GetVariableListByActivity(
                kit,
                taskView.ProcessInstanceId.Value,
                taskView.ActivityInstanceId.Value,
                requiredNames) ?? new List<ProcessVariableEntity>();
            var byName = loaded
                .Where(e => e != null && !string.IsNullOrEmpty(e.Name))
                .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var name in requiredNames)
            {
                if (!byName.TryGetValue(name, out var ovDbEntity) || ovDbEntity == null)
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
