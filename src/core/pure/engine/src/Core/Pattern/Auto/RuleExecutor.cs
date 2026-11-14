using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using sailflow.data;
using mooSQL.data;
using sailflow.engine.Entity;
using sailflow.engine.Manager;
using sailflow.engine.Common;
using sailflow.engine.Event;
using sailflow.engine.Executor;
using sailflow.engine.Xpdl;
using sailflow.engine.Client;
using sailflow.WebUtility;
using sailflow.engine.Core;

using sailflow.engine.Factories;

namespace sailflow.engine.Pattern
{
    internal static class RuleExecutor
    {
        internal static void ExecuteRule(SailClient client,
            RuleConfigDetail ruleConfig,
            Activity currentActivity,
            ActivityInstanceEntity currentActivityInstance,
            IEventService eventService)
        {
            if (ruleConfig == null || string.IsNullOrWhiteSpace(ruleConfig.RuleSetCode))
                return;

            var ruleSet = client.ManagerFactory.RuleSets.GetByCode(ruleConfig.RuleSetCode);
            var inputs = BuildRuleInputs(client, eventService, currentActivityInstance, currentActivity);
            var result = client.ManagerFactory.RuleSetExecutions.Execute(ruleSet, inputs);

            if (result.IsSuccess != true)
                throw new WorkflowException($"RuleTask execution failed: {result.Message}");

            if (result.OutputVariables == null || result.OutputVariables.Count == 0)
                return;

            var processInstanceId = eventService.GetProcessInstanceId();
            var kit = eventService.GetKit();
            var processInstance = client.ProcessInstances.GetById(kit, processInstanceId);
            var pvm = SailFactoryAccess.Managers(client).ProcessVariables;

            foreach (var item in result.OutputVariables)
            {
                if (string.IsNullOrWhiteSpace(item.Key)) continue;
                var workflowVarName = item.Key.Trim();
                var value = item.Value == null ? string.Empty : Convert.ToString(item.Value, CultureInfo.InvariantCulture);

                var processVariable = new ProcessVariableEntity
                {
                    VariableScope = ProcessVariableScopeEnum.Activity.ToString(),
                    AppInstanceId = processInstance?.AppInstanceId,
                    ProcessId = processInstance?.ProcessId,
                    ProcessInstanceId = processInstanceId,
                    ActivityInstanceId = currentActivityInstance.ActivityInstanceId,
                    ActivityId = currentActivity.ActivityId,
                    ActivityName = currentActivity.ActivityName,
                    Name = workflowVarName,
                    Value = value,
                    MediaType = MultiMediaTypeEnum.Text.ToString(),
                    UpdatedDateTime = DateTime.UtcNow
                };
                pvm.SaveVariable(kit, processVariable);
            }
        }

        /// <summary>
        /// Rule inputs: process-scoped vars; then <c>wf_variable</c> for this activity (Input rows —
        /// referenced inputs load from prior node <c>wf_proc_variable</c> via source_ref / source_variable_name);
        /// if no <c>wf_variable</c> rows, merge all activity-scoped rows for this instance (legacy).
        /// Auto-run uses <see cref="AutoRunEventServiceAdapter.VariablesSnapshot"/>.
        /// </summary>
        private static IDictionary<string, object> BuildRuleInputs(SailClient client, IEventService eventService, ActivityInstanceEntity activityInstance, Activity currentActivity)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (eventService is AutoRunEventServiceAdapter autoRun)
            {
                var bag = autoRun.VariablesSnapshot;
                if (bag == null) return dict;
                foreach (var kv in bag)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                    dict[kv.Key.Trim()] = ParseTypedValue(kv.Value ?? string.Empty);
                }
                return dict;
            }

            var kit = eventService.GetKit();
            if (kit == null || activityInstance == null || currentActivity == null)
                return dict;
            var pvm = SailFactoryAccess.Managers(client).ProcessVariables;
            var processInstanceId = eventService.GetProcessInstanceId();
            var activityInstanceId = activityInstance.ActivityInstanceId;
            var processScope = ProcessVariableScopeEnum.Process.ToString();
            var activityScope = ProcessVariableScopeEnum.Activity.ToString();

            var allForProcess = pvm.GetVariableList(kit, processInstanceId);
            if (allForProcess != null)
            {
                foreach (var v in allForProcess)
                {
                    if (string.IsNullOrWhiteSpace(v?.Name)) continue;
                    if (!string.Equals(v.VariableScope, processScope, StringComparison.OrdinalIgnoreCase))
                        continue;
                    dict[v.Name.Trim()] = ParseTypedValue(v.Value ?? string.Empty);
                }
            }

            var processInstance = client.ProcessInstances.GetById(kit, processInstanceId);
            if (processInstance != null
                && !string.IsNullOrWhiteSpace(processInstance.ProcessId)
                && !string.IsNullOrWhiteSpace(processInstance.Version)
                && !string.IsNullOrWhiteSpace(currentActivity.ActivityId))
            {
                var vm = client.ManagerFactory.Variables;
                var defs = vm.GetList(processInstance.ProcessId, processInstance.Version, currentActivity.ActivityId);
                var inputDefs = defs?.Where(IsWfVariableInput).ToList() ?? new List<VariableEntity>();
                if (inputDefs.Count > 0)
                {
                    var aim = client.ManagerFactory.Activities;
                    foreach (var def in inputDefs)
                    {
                        var key = (def.Name ?? string.Empty).Trim();
                        if (string.IsNullOrEmpty(key)) continue;

                        string raw = null;
                        if (def.IsReferenced == 1
                            && !string.IsNullOrWhiteSpace(def.SourceRef)
                            && !string.IsNullOrWhiteSpace(def.SourceVariableName))
                        {
                            var sourceInst = ResolveSourceActivityInstance(activityInstance, def.SourceRef.Trim(), kit, aim);
                            if (sourceInst != null)
                            {
                                var srcEntity = pvm.GetVariableByActivity(kit, processInstanceId, sourceInst.ActivityInstanceId,
                                    def.SourceVariableName.Trim());
                                raw = srcEntity?.Value;
                            }
                        }
                        else
                        {
                            var local = pvm.GetVariableByActivity(kit, processInstanceId, activityInstanceId, key);
                            raw = local?.Value;
                            if (raw == null && !string.IsNullOrEmpty(def.DefaultValue))
                                raw = def.DefaultValue;
                        }

                        // Keep declared input variables in the input bag even when source value is missing,
                        // so JSON DSL expressions (e.g. LeaveType == "Sick") can evaluate against empty string.
                        dict[key] = ParseTypedValue(raw ?? string.Empty);
                    }

                    return dict;
                }
            }

            var activityRows = pvm.GetVariableList(kit, processInstanceId, activityInstanceId);
            if (activityRows != null)
            {
                foreach (var v in activityRows)
                {
                    if (string.IsNullOrWhiteSpace(v?.Name)) continue;
                    if (!string.Equals(v.VariableScope, activityScope, StringComparison.OrdinalIgnoreCase))
                        continue;
                    dict[v.Name.Trim()] = ParseTypedValue(v.Value ?? string.Empty);
                }
            }

            return dict;
        }

        private static bool IsWfVariableInput(VariableEntity e)
        {
            if (e == null) return false;
            if (string.IsNullOrWhiteSpace(e.Direction)) return true;
            return !string.Equals(e.Direction, "Output", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Prefer token-aware previous instance; else latest completed instance for <paramref name="sourceActivityId"/>.
        /// </summary>
        private static ActivityInstanceEntity ResolveSourceActivityInstance(
            ActivityInstanceEntity currentInstance,
            string sourceActivityId,
            SQLBuilder kit,
            ActivityInstanceManager aim)
        {
            var prev = aim.GetPreviousActivityInstanceSimple(kit.useSQL(), currentInstance, sourceActivityId);
            if (prev != null) return prev;

            var completed = aim.GetActivityInstanceListCompletedSimple(kit.useSQL(), currentInstance.ProcessInstanceId, sourceActivityId);
            return completed != null && completed.Count > 0 ? completed[0] : null;
        }

        private static object ParseTypedValue(string raw)
        {
            if (bool.TryParse(raw, out var b)) return b;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i;
            if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) return l;
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)) return d;
            if (double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var f)) return f;
            return raw;
        }
    }
}

