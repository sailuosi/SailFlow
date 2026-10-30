using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using saiflow.engine.Common;
using saiflow.engine.Business.Entity;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Xpdl.Node;
using saiflow.engine.Utility;
using System.Xml.Linq;

namespace saiflow.engine.Xpdl
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
            var convert = ConvertorFactory.CreateConvertor(xmlNode, xnpmgr, out activityType);
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
    }
}
