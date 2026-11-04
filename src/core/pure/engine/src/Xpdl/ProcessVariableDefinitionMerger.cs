using System.Collections.Generic;
using System.Linq;
using saiflow.engine.Manager;
using saiflow.engine.Client;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Xpdl
{
    /// <summary>
    /// Injects activity variable definitions from <c>wf_variable</c> after BPMN XML is parsed.
    /// Node variable metadata is no longer read from <c>sf:variables</c> in XML.
    /// </summary>
    public static class ProcessVariableDefinitionMerger
    {
        public static void ApplyFromDatabase(SailClient client, Process process, string processId, string version)
        {
            if (process == null || string.IsNullOrEmpty(processId))
                return;

            if (string.IsNullOrEmpty(version))
                version = "1";

            ApplyToActivities(client, process.ActivityList, processId, version);
        }

        private static void ApplyToActivities(SailClient client, IList<Activity> activities, string processId, string version)
        {
            if (activities == null)
                return;

            var vm = new VariableManager(client);

            foreach (var activity in activities)
            {
                var rows = vm.GetList(processId, version, activity.ActivityId);
                if (rows != null && rows.Count > 0)
                {
                    activity.VariableList = rows
                        .Select(VariableDefinitionMapper.ToVariableDetail)
                        .Where(v => v != null)
                        .ToList();
                }

                if (activity.Node is SubProcessNode spn && spn.SubProcessNested != null)
                {
                    var subProcessId = !string.IsNullOrEmpty(spn.SubProcessNested.ProcessId)
                        ? spn.SubProcessNested.ProcessId
                        : processId;

                    ApplyFromDatabase(client, spn.SubProcessNested, subProcessId, version);
                }
            }
        }
    }
}
