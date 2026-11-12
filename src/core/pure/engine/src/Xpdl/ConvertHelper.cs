using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;
using sailflow.engine.Utility;
using System.Xml.Linq;

namespace sailflow.engine.Xpdl
{
    /// <summary>
    /// Convert Helper
    /// </summary>
    public class ConvertHelper
    {
        #region Activity Convert
        /// <summary>
        /// Convert XmlNode to Activity Entity
        /// </summary>
        public static Activity ConvertXmlActivityNodeToActivityEntity(XmlNode xmlNode, 
            XmlNamespaceManager xnpmgr, 
            string processId)
        {
            if (xmlNode == null)
            {
                throw new WfXpdlException("The xml node can't be null, please check the xml file");
            }

            ActivityTypeEnum activityType = ActivityTypeEnum.Unknown;
            var convert = CreateConvertor(xmlNode, xnpmgr, out activityType);
            Activity entity = convert.Convert();
            entity.ProcessId = processId;
            entity.ActivityType = activityType;

            return entity;
        }
        #endregion

        #region Form Node Convert
        /// <summary>
        /// Convert nodt to Form
        /// </summary>
        public static FormOuter ComnvertXmlFormNodeToFormEntity(XmlNode xmlNode,
            XmlNamespaceManager xnpmgr,
            string processId)
        {
            if (xmlNode == null)
            {
                throw new WfXpdlException("The xml node can't be null, please check the xml file");
            }

            var form = new FormOuter();
            form.OuterId = XMLHelper.GetXmlAttribute(xmlNode, "outerId");
            form.OuterCode = XMLHelper.GetXmlAttribute(xmlNode, "outerCode");
            form.Name = XMLHelper.GetXmlAttribute(xmlNode, "name");
            return form;

        }
        #endregion
        internal static IConvert CreateConvertor(XmlNode xmlNode, XmlNamespaceManager xnpmgr, out ActivityTypeEnum activityType)
        {
            IConvert convert = null;
            activityType = GetActivityTypeByNodeName(xmlNode);
            if (activityType == ActivityTypeEnum.Unknown)
            {
                throw new ApplicationException(String.Format("ConvertHelper:Not suppported convertor as of node type:{0}",
                    activityType.ToString()));
            }
            else if (activityType == ActivityTypeEnum.GatewayNode)
            {
                convert = new GatewayConvertor(xmlNode, xnpmgr);
            }
            else if(activityType == ActivityTypeEnum.SubProcessNode)
            {
                convert = new SubProcessConvertor(xmlNode, xnpmgr);
            }
            else if(activityType == ActivityTypeEnum.MultiSignNode)
            {
                convert = new MultiSignConvertor(xmlNode, xnpmgr);
            }
            else if(activityType == ActivityTypeEnum.StartNode
                || activityType == ActivityTypeEnum.IntermediateNode
                || activityType == ActivityTypeEnum.EndNode)
            {
                convert = new EventConvertor(xmlNode, xnpmgr);
            }
            else
            {
                convert = new DefaultlConvertor(xmlNode, xnpmgr);
            }
            return convert;
        }

        static ActivityTypeEnum GetActivityTypeByNodeName(XmlNode node)
        {
            ActivityTypeEnum activityType = ActivityTypeEnum.Unknown;
            if (node.Name == XPDLDefinition.BPMN_ElementName_StartEvent)
            {
                activityType = ActivityTypeEnum.StartNode;
            }
            else if (node.Name == XPDLDefinition.BPMN_ElementName_EndEvent)
            {
                activityType = ActivityTypeEnum.EndNode;
            }
            else if (node.Name == XPDLDefinition.BPMN_ElementName_IntermediateEvent_Catch
                || node.Name == XPDLDefinition.BPMN_ElementName_IntermediateEvent_Throw)
            {
                activityType = ActivityTypeEnum.IntermediateNode;
            }
            else if (node.Name == XPDLDefinition.BPMN_ElementName_SubProcess)
            {
                activityType = ActivityTypeEnum.SubProcessNode;
            }
            else if (node.Name == XPDLDefinition.BPMN_ElementName_Task
                || node.Name == XPDLDefinition.BPMNElementNameUserTask
                || node.Name == XPDLDefinition.BPMN_ElementName_ManualTask)
            {
                var multipleDetailNode = node.SelectSingleNode(XPDLDefinition.Sf_StrXmlPath_MultiSignDetail, 
                    XPDLHelper.GetSailflowXmlNamespaceManager(node.OwnerDocument));
                if (multipleDetailNode != null) activityType = ActivityTypeEnum.MultiSignNode;
                else activityType = ActivityTypeEnum.TaskNode;
            }
            else if(node.Name == XPDLDefinition.BPMN_ElementName_ServiceTask
                || node.Name == XPDLDefinition.BPMN_ElementName_BusinessRuleTask)
            {
                var nsmgr = XPDLHelper.GetSailflowXmlNamespaceManager(node.OwnerDocument);
                var aiServicesNode = node.SelectSingleNode(XPDLDefinition.Sf_StrXmlPath_AiServices, nsmgr);
                if (aiServicesNode != null)
                {
                    activityType = ActivityTypeEnum.AIServiceNode;
                }
                else
                {
                    activityType = ActivityTypeEnum.ServiceNode;
                }
            }
            else if(node.Name == XPDLDefinition.BPMN_ElementName_ScriptTask)
            {
                activityType = ActivityTypeEnum.ScriptNode;
            }
            else if (node.Name == XPDLDefinition.BPMN_ElementName_ExclusiveGateway
                || node.Name == XPDLDefinition.BPMN_ElementName_InclusiveGateway
                || node.Name == XPDLDefinition.BPMN_ElementName_ParallelGateway)
            {
                activityType = ActivityTypeEnum.GatewayNode;
            }
            else
            {
                throw new ApplicationException(string.Format("ConvertHelper:Not supported node name:{0}", node.Name));
            }
            return activityType;
        }
    }
}
