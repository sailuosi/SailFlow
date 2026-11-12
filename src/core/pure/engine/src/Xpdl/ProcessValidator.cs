using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using sailflow.engine.Common;
using sailflow.engine.Xpdl;
using System.Xml;
using sailflow.engine.Utility;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Xpdl
{
    /// <summary>
    /// Process Validator
    /// </summary>
    internal class ProcessValidator
    {
        /// <summary>
        /// Validate
        /// </summary>
        internal static ProcessValidateResult Validate(ProcessEntity entity)
        {
            var result = new ProcessValidateResult();
            var xmlContent = entity.XmlContent;
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);

            var root = xmlDoc.DocumentElement;
            var startEvent = XMLHelper.GetXmlNodeByXpath(xmlDoc, XPDLDefinition.BPMN_StrXmlPath_StartEvent,
                XPDLHelper.GetSailflowXmlNamespaceManager(xmlDoc));
            var endEvent = XMLHelper.GetXmlNodeByXpath(xmlDoc, XPDLDefinition.BPMN_StrXmlPath_EndEvent,
                XPDLHelper.GetSailflowXmlNamespaceManager(xmlDoc));

            //检验开始节点和结束节点
            //Check the start and end nodes
            if (startEvent == null)
            {
                result.ProcessValidatedResultType = ProcessValidateResultTypeEnum.WithoutStartEvent;
                return result;
            }
            else if (endEvent == null)
            {
                result.ProcessValidatedResultType = ProcessValidateResultTypeEnum.WithoutEndEvent;
                return result;
            }

            //检验开始到结束的关键路径是否存在
            //Check if there is a critical path from start to finish
            var process = ProcessModelHelper.ConvertToXPDLProcess(xmlDoc);
            if (process == null)
            {
                result.ProcessValidatedResultType = ProcessValidateResultTypeEnum.WithoutStartEndPath;
                result.Message = "BPMN process node missing or unreadable.";
                return result;
            }

            var dangling = FindDanglingTransitionTargets(process);
            if (dangling.Count > 0)
            {
                result.ProcessValidatedResultType = ProcessValidateResultTypeEnum.WithoutStartEndPath;
                result.Issues = dangling;
                result.Message = string.Join("; ", dangling);
                return result;
            }

            var startActivityId = XMLHelper.GetXmlAttribute(startEvent, "id");
            var activityList = process.ActivityList?.ToList() ?? new List<Activity>();
            var transitionList = process.TransitionList?.ToList() ?? new List<Transition>();
            activityList = activityList.Where(a => a != null && a.ActivityId != startActivityId).ToList();

            var isExistedStartEndPath = TranverseTransitonList(process, activityList, transitionList, startActivityId);
            if (isExistedStartEndPath == false)
            {
                result.ProcessValidatedResultType = ProcessValidateResultTypeEnum.WithoutStartEndPath;
                return result;
            }

            // R6：用户任务发布前 Resolve + Leave Type 均不得为空
            var assigneeIssues = ValidateUserTaskAssigneeConfig(process.ActivityList);
            if (assigneeIssues.Count > 0)
            {
                result.ProcessValidatedResultType = ProcessValidateResultTypeEnum.AssigneeConfigIncomplete;
                result.Issues = assigneeIssues;
                result.Message = string.Join("; ", assigneeIssues);
                return result;
            }

            result.ProcessValidatedResultType = ProcessValidateResultTypeEnum.Successed;
            return result;
        }

        /// <summary>R6：人工任务须具备 AssigneeResolve + LeaveJudge Type。</summary>
        internal static IList<string> ValidateUserTaskAssigneeConfig(IList<Activity> activities)
        {
            var issues = new List<string>();
            if (activities == null)
                return issues;

            foreach (var a in activities)
            {
                if (a == null || a.ActivityType != ActivityTypeEnum.TaskNode)
                    continue;

                var label = string.IsNullOrWhiteSpace(a.ActivityName)
                    ? a.ActivityId
                    : a.ActivityName + "(" + a.ActivityId + ")";

                if (string.IsNullOrWhiteSpace(a.AssigneeResolveType))
                    issues.Add("UserTask '" + label + "' missing AssigneeResolve Type");
                if (string.IsNullOrWhiteSpace(a.LeaveJudgeType))
                    issues.Add("UserTask '" + label + "' missing LeaveJudge Type");
            }

            return issues;
        }

        /// <summary>
        /// 连线 targetRef 指向未在 process 中声明的活动（仅有 BPMNDI 形状、无 bpmn:task 等）。
        /// </summary>
        internal static IList<string> FindDanglingTransitionTargets(Process process)
        {
            var issues = new List<string>();
            if (process?.TransitionList == null)
                return issues;

            var known = new HashSet<string>(
                (process.ActivityList ?? Array.Empty<Activity>())
                    .Where(a => a != null && !string.IsNullOrWhiteSpace(a.ActivityId))
                    .Select(a => a.ActivityId),
                StringComparer.Ordinal);

            foreach (var t in process.TransitionList)
            {
                if (t == null || string.IsNullOrWhiteSpace(t.ToActivityId))
                    continue;
                if (known.Contains(t.ToActivityId))
                    continue;
                issues.Add(
                    "Transition '" + (t.TransitionId ?? "?")
                    + "' targetRef='" + t.ToActivityId
                    + "' has no matching activity element in process.");
            }
            return issues;
        }

        /// <summary>
        /// Iterative verification method
        /// 迭代验证方法
        /// </summary>
        internal static Boolean TranverseTransitonList(Process process,
            IList<Activity> activityList,
            IList<Transition> transitionList,
            string activityId)
        {
            if (process == null || transitionList == null || string.IsNullOrEmpty(activityId))
                return false;

            var list = transitionList.Where(t => t != null && t.FromActivityId == activityId).ToList();
            foreach (var t in list)
            {
                var toActivityId = t.ToActivityId;
                var activity = ProcessModelHelper.GetActivity(process, toActivityId);
                // 断链 / 仅 DI 无元素：跳过该边，避免 NRE
                if (activity == null)
                    continue;
                if (activity.ActivityType == ActivityTypeEnum.EndNode)
                {
                    return true;
                }
                activityList = (activityList ?? Array.Empty<Activity>())
                    .Where(a => a != null && a.ActivityId != toActivityId)
                    .ToList();
                var isStartEndExisted = TranverseTransitonList(process, activityList, transitionList, toActivityId);
                if (isStartEndExisted == true)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
