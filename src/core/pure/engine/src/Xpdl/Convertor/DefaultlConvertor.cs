using saiflow.engine.Xpdl.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using saiflow.engine.Common;
using saiflow.engine.Utility;
using saiflow.engine.Xpdl.Common;

namespace saiflow.engine.Xpdl.Convertor
{
    /// <summary>
    /// Default Convertor
    /// </summary>
    internal class DefaultlConvertor : ConvertorBase, IConvert
    {
        public DefaultlConvertor(XmlNode node, XmlNamespaceManager xnpmgr) : base(node, xnpmgr)
        {
        }

        public override Activity ConvertElementDetail(Activity entity)
        {
            return entity;
        }
    }
}
