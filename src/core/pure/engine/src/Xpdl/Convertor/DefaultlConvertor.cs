using sailflow.engine.Xpdl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using sailflow.engine.Common;
using sailflow.engine.Utility;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Xpdl
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
