using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sailflow.engine.Common;
using sailflow.engine.Utility;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Xpdl
{
    /// <summary>
    /// Convert Base
    /// 转换器基类
    /// </summary>
    public abstract class ConvertorBase : IConvert
    {
        #region Property and Constructor
        abstract public Activity ConvertElementDetail(Activity entity);
        private XmlNode xmlNode;
        public XmlNode XMLNode
        {
            get
            {
                return xmlNode;
            }
        }

        private XmlNamespaceManager xmlNamespaceManager;
        public XmlNamespaceManager XMLNamespaceManager
        {
            get
            {
                return xmlNamespaceManager;
            }
        }

        public ConvertorBase(XmlNode node, XmlNamespaceManager xnmgr)
        {
            xmlNode = node;
            xmlNamespaceManager = xnmgr;
        }
        #endregion

        /// <summary>
        /// Convert
        /// </summary>
        /// <returns></returns>
        public Activity Convert()
        {
            var activity = new Activity();
            if (XMLNode == null || XMLNamespaceManager == null || activity == null)
                throw new ArgumentNullException("Argument can't be null when converting xmlnode to activity entity");
            var convertorBuilder = new ConvertorBuilder(XMLNode, XMLNamespaceManager, activity);
            convertorBuilder.ConvertGeneral()
                .ConvertPartakers()
                .ConvertAssigneeConfig()
                .ConvertActions()
                .ConvertSections()
                .ConvertServices()
                .ConvertAIServices()
                .ConvertBoundires()
                .ConvertRuleConfigs()
                .ConvertVariables()
                .ConvertNotifications();

            ConvertElementDetail(activity);
            return activity;
        }
    }
}
