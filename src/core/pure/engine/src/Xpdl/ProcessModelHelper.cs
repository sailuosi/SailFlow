using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;
using sailflow.engine.Utility;

namespace sailflow.engine.Xpdl
{
    /// <summary>
    /// Process Model Helper
    /// </summary>
    public class ProcessModelHelper
    {
        #region Activity Veiw Convertor
        /// <summary>
        /// Get start activity
        /// </summary>
        public static Activity GetStartActivity(Process process)
        {
            var activityList = process.ActivityList.ToList();
            var activity = activityList.SingleOrDefault<Activity>(a => a.ActivityType == ActivityTypeEnum.StartNode);
            return activity;
        }

        /// <summary>
        /// Get end activity
        /// </summary>
        public static Activity GetEndActivity(Process process)
        {
            var activityList = process.ActivityList.ToList();
            var activity = activityList.FirstOrDefault<Activity>(a => a.ActivityType == ActivityTypeEnum.EndNode);
            return activity;
        }

        /// <summary>
        /// Get activity
        /// </summary>
        public static Activity GetActivity(Process process, string activityId)
        {
            if (process?.ActivityList == null || string.IsNullOrEmpty(activityId))
                return null;
            var activityList = process.ActivityList.ToList();
            var activity = activityList.SingleOrDefault<Activity>(a => a.ActivityId == activityId);
            return activity;
        }

        /// <summary>
        /// Get first activity
        /// </summary>
        public static Activity GetFirstActivity(Process process)
        {
            var activityList = process.ActivityList.ToList();
            var activity = activityList.SingleOrDefault<Activity>(a => a.ActivityType == ActivityTypeEnum.StartNode);
            if (activity == null)
                return null;

            // 开始事件偶发多出边时取第一条，避免 SingleOrDefault 抛异常
            var transition = process.TransitionList?
                .FirstOrDefault(t => t != null && t.FromActivityId == activity.ActivityId);
            return transition?.ToActivity;
        }

        /// <summary>
        /// Get Next Activity by from（仅适用于唯一出边；多出口请用 GetForwardTransitionList）。
        /// 多出边时返回 null，避免 SingleOrDefault 抛 “Sequence contains more than one matching element”。
        /// </summary>
        public static Activity GetNextActivity(Process process, string fromActivityId)
        {
            if (process?.TransitionList == null || string.IsNullOrEmpty(fromActivityId))
                return null;
            var matches = process.TransitionList
                .Where(t => t != null && t.FromActivityId == fromActivityId)
                .ToList();
            if (matches.Count != 1)
                return null;
            return matches[0].ToActivity;
        }

        /// <summary>
        /// Convert to activity view
        /// </summary>
        public static ActivityView ConvertFromActivityEntity(Activity entity)
        {
            var view = new ActivityView();
            view.ActivityId = entity.ActivityId;
            view.ActivityName = entity.ActivityName;
            view.ActivityCode = entity.ActivityCode;
            view.ActivityType = entity.ActivityType.ToString();
            if (entity.TriggerDetail != null)
            {
                view.TriggerType = entity.TriggerDetail.TriggerType.ToString();
                view.MessageDirection = entity.TriggerDetail.MessageDirection.ToString();
                view.Expression = entity.TriggerDetail.Expression;
            }
            return view;
        }
        #endregion

        #region Transition Convertor
        /// <summary>
        /// Get transition list
        /// </summary>
        public static IList<Transition> GetForwardTransitionList(Process process, string fromActivityId)
        {
            var transitionList = process.TransitionList.Where<Transition>(t => t.FromActivityId == fromActivityId).ToList();
            return transitionList;
        }

        /// <summary>
        /// Get transition
        /// </summary>
        public static Transition GetForwardTransition(Process process, string fromActivityId, string toActivityId)
        {
            var transiton = process.TransitionList.SingleOrDefault<Transition>(t => t.FromActivityId == fromActivityId
                && t.ToActivityId == toActivityId);
            return transiton;
        }

        /// <summary>
        /// Get backward transition list
        /// </summary>
        public static IList<Transition> GetBackwardTransitionList(Process process, string toActivityId)
        {
            var transitionList = process.TransitionList.Where<Transition>(t => t.ToActivityId == toActivityId).ToList();
            return transitionList;
        }
        #endregion

        #region Convert process property
        /// <summary>
        /// Convert to process file entity
        /// </summary>
        public static ProcessFileEntity ConvertFromXmlNodeProcess(XmlNode xmlNodeProcess)
        {
            var process = new ProcessFileEntity();
            process.ProcessId = XMLHelper.GetXmlAttribute(xmlNodeProcess, "id");
            process.ProcessName = XMLHelper.GetXmlAttribute(xmlNodeProcess, "name");
            if (string.IsNullOrEmpty(process.ProcessName)) process.ProcessName = process.ProcessId;

            process.ProcessCode = XMLHelper.GetXmlAttribute(xmlNodeProcess, "sf:code");
            process.Version = "1";
            process.Status = 1;

            return process;
        }

        /// <summary>
        /// Convert to process
        /// </summary>
        public static sailflow.engine.Xpdl.Process ConvertToXPDLProcess(XmlDocument xmlDoc)
        {
            var root = xmlDoc.DocumentElement;
            var xmlNodeProcess = root.SelectSingleNode(XPDLDefinition.BPMN_StrXmlPath_Process,
                    XPDLHelper.GetSailflowXmlNamespaceManager(xmlDoc));
            var process = ProcessModelConvertor.ConvertProcessModelFromXML(xmlNodeProcess);
            return process;
        }
        #endregion
    }
}
