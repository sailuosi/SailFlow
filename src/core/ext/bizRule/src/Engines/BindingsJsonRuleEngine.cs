using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using saiflow.engine.Entity;
using saiflow.engine.Common;
using saiflow.engine.Rules;
using saiflow.engine.Xpdl;

namespace saiflow.hub.BusinessRule
{
    /// <summary>
    /// 内置规则引擎：JSON DSL（<c>rules</c> / when / set），mode = <c>bindingsJson</c>。
    /// </summary>
    public sealed class BindingsJsonRuleEngine : IRuleEngine
    {
        public string Mode => RuleSetModeHelper.PersistedBindingsJson;

        public RuleExecutionResultEntity Execute(RuleSetEntity ruleSet, IDictionary<string, object> variables)
        {
            var outputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(ruleSet?.RuleContent))
            {
                return Fail("rule_content is empty", outputs);
            }

            JToken root;
            try
            {
                root = JToken.Parse(ruleSet.RuleContent);
            }
            catch (Exception ex)
            {
                return Fail("Invalid rule_content JSON: " + ex.Message, outputs);
            }

            var stringDict = ToStringDictionary(variables);
            var rules = root["rules"] as JArray;
            if (rules == null || rules.Count == 0)
            {
                return Fail("bindingsJson mode requires a \"rules\" array in rule_content", outputs);
            }

            var matched = false;
            foreach (var rule in rules)
            {
                var when = rule["when"]?.ToString();
                if (!string.IsNullOrWhiteSpace(when))
                {
                    if (!EvaluateWhenExpression(when, stringDict))
                        continue;
                }

                MergeRuleOutputs(rule, outputs);
                matched = true;
                break;
            }

            if (!matched && root["default"] is JObject defObj)
                MergeJObjectIntoOutputs(defObj, outputs);

            return new RuleExecutionResultEntity
            {
                IsSuccess = true,
                Message = "OK",
                OutputVariables = outputs
            };
        }

        public bool ValidateRuleContent(string ruleContent, out string message)
        {
            message = null;
            if (string.IsNullOrWhiteSpace(ruleContent))
            {
                message = "rule_content is required for bindingsJson mode";
                return false;
            }

            try
            {
                var token = JToken.Parse(ruleContent);
                var rules = token["rules"] as JArray;
                if (rules == null || rules.Count == 0)
                {
                    message = "bindingsJson mode requires rule_content with a non-empty \"rules\" array";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        private static void MergeRuleOutputs(JToken rule, IDictionary<string, object> outputs)
        {
            if (rule["set"] is JObject j1) MergeJObjectIntoOutputs(j1, outputs);
            if (rule["then"] is JObject j2) MergeJObjectIntoOutputs(j2, outputs);
            if (rule["outputs"] is JObject j3) MergeJObjectIntoOutputs(j3, outputs);
        }

        private static void MergeJObjectIntoOutputs(JObject jo, IDictionary<string, object> outputs)
        {
            foreach (var p in jo.Properties())
                outputs[p.Name] = JTokenToOutputValue(p.Value);
        }

        private static object JTokenToOutputValue(JToken t)
        {
            if (t == null || t.Type == JTokenType.Null)
                return string.Empty;
            switch (t.Type)
            {
                case JTokenType.Integer:
                    return t.Value<long>();
                case JTokenType.Float:
                    return t.Value<decimal>();
                case JTokenType.Boolean:
                    return t.Value<bool>();
                case JTokenType.String:
                    return t.Value<string>() ?? string.Empty;
                default:
                    return t.ToString();
            }
        }

        private static bool EvaluateWhenExpression(string when, IDictionary<string, string> stringDict)
        {
            try
            {
                var replaced = ExpressionParser.ReplaceParameterToValue(when, stringDict);
                return ExpressionParser.Parse(replaced);
            }
            catch
            {
                return false;
            }
        }

        private static IDictionary<string, string> ToStringDictionary(IDictionary<string, object> variables)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (variables == null) return d;
            foreach (var kv in variables)
                d[kv.Key] = kv.Value == null
                    ? string.Empty
                    : Convert.ToString(kv.Value, CultureInfo.InvariantCulture);
            return d;
        }

        private static RuleExecutionResultEntity Fail(string message, IDictionary<string, object> outputs) =>
            new RuleExecutionResultEntity
            {
                IsSuccess = false,
                Message = message,
                OutputVariables = outputs
            };
    }
}
