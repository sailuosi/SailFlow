using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using saiflow.engine.Common;
using saiflow.engine.Utility;
using saiflow.engine.Xpdl;
using saiflow.WebUtility;


namespace saiflow.engine.Xpdl
{
    /// <summary>
    /// Multiple Instance Convertor
    /// </summary>
    public class MultiSignConvertor : ConvertorBase, IConvert
    {
        public MultiSignConvertor(XmlNode node, XmlNamespaceManager xnpmgr) : base(node, xnpmgr)
        {
        }

        public override Activity ConvertElementDetail(Activity entity)
        {
            var multiSignDetail = new MultiSignDetail();
            var multipleNode = this.XMLNode.SelectSingleNode(XPDLDefinition.Sf_StrXmlPath_MultiSignDetail, base.XMLNamespaceManager);

            if (!string.IsNullOrEmpty(XMLHelper.GetXmlAttribute(multipleNode, "complexType")))
            {
                multiSignDetail.ComplexType = EnumHelper.ParseEnum<ComplexTypeEnum>(XMLHelper.GetXmlAttribute(multipleNode, "complexType"));
            }

            if (!string.IsNullOrEmpty(XMLHelper.GetXmlAttribute(multipleNode, "mergeType")))
            {
                multiSignDetail.MergeType = EnumHelper.ParseEnum<MergeTypeEnum>(XMLHelper.GetXmlAttribute(multipleNode, "mergeType"));
            }

            if (!string.IsNullOrEmpty(XMLHelper.GetXmlAttribute(multipleNode, "compareType")))
            {
                multiSignDetail.CompareType = EnumHelper.ParseEnum<CompareTypeEnum>(XMLHelper.GetXmlAttribute(multipleNode, "compareType"));
            }

            if (!string.IsNullOrEmpty(XMLHelper.GetXmlAttribute(multipleNode, "completeOrder")))
            {
                multiSignDetail.CompleteOrder = float.Parse(XMLHelper.GetXmlAttribute(multipleNode, "completeOrder"));
            }
            entity.MultiSignDetail = multiSignDetail;

            var multipleInstanceNode = new MultipleInstanceNode(entity);
            entity.Node = multipleInstanceNode;

            return entity;
        }
    }
}
