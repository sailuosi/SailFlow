using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NRules;
using NRules.Fluent;
using sailflow.engine.Entity;
using sailflow.engine.Common;
using sailflow.engine.Rules;

namespace sailflow.hub.BusinessRule
{
    /// <summary>
    /// 内置规则引擎：NRules Fluent（<c>ruleTypes</c> 类型名列表），mode = <c>ruleTypes</c>。
    /// </summary>
    public sealed class NRulesRuleTypesEngine : IRuleEngine
    {
        public string Mode => RuleSetModeHelper.PersistedRuleTypes;

        public RuleExecutionResultEntity Execute(RuleSetEntity ruleSet, IDictionary<string, object> variables)
        {
            var ruleTypes = ParseRuleTypes(ruleSet?.RuleContent);
            if (ruleTypes.Count == 0)
            {
                return Fail("No ruleTypes configured in rule_content");
            }

            var loadedTypes = ResolveRuleTypes(ruleTypes);
            if (loadedTypes.Count == 0)
            {
                return Fail("No rule types could be loaded");
            }

            var repository = new RuleRepository();
            repository.Load(x => x.From(loadedTypes.ToArray()));
            var factory = repository.Compile();
            var session = factory.CreateSession();

            var inputFact = new RuleInputFact { Vars = variables ?? new Dictionary<string, object>() };
            var outputFact = new RuleOutputFact();

            session.Insert(inputFact);
            session.Insert(outputFact);
            session.Fire();

            return new RuleExecutionResultEntity
            {
                IsSuccess = true,
                Message = "OK",
                OutputVariables = outputFact.Vars ?? new Dictionary<string, object>()
            };
        }

        public bool ValidateRuleContent(string ruleContent, out string message)
        {
            message = null;
            try
            {
                var types = ParseRuleTypes(ruleContent);
                if (types.Count == 0)
                {
                    message = "ruleTypes mode requires rule_content with a non-empty \"ruleTypes\" array";
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

        private static IList<string> ParseRuleTypes(string ruleContent)
        {
            if (string.IsNullOrWhiteSpace(ruleContent)) return new List<string>();
            try
            {
                var token = JToken.Parse(ruleContent);
                var arr = token["ruleTypes"] as JArray;
                if (arr == null) return new List<string>();
                return arr.Select(x => x?.ToString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static IList<Type> ResolveRuleTypes(IList<string> ruleTypeNames)
        {
            var list = new List<Type>();
            foreach (var typeName in ruleTypeNames)
            {
                var t = Type.GetType(typeName, false);
                if (t == null)
                {
                    t = AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => a.GetType(typeName, false))
                        .FirstOrDefault(x => x != null);
                }
                if (t != null)
                    list.Add(t);
            }
            return list;
        }

        private static RuleExecutionResultEntity Fail(string message) =>
            new RuleExecutionResultEntity
            {
                IsSuccess = false,
                Message = message,
                OutputVariables = new Dictionary<string, object>()
            };
    }
}
