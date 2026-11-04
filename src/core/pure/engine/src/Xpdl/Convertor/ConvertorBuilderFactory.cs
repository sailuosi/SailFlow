using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Xpdl
{
    public class ConvertorBuilderFactory
    {
        /// <summary>
        /// Create method
        /// </summary>
        public static ConvertorBuilder Create(XmlNode xmlNode, XmlNamespaceManager xnpmgr, Activity activityEntity)
        {
            if (xmlNode == null
                || xnpmgr == null
                || activityEntity == null)
            {
                throw new ArgumentNullException("Argument can't be null when converting xmlnode to activity entity");
            }

            var builder = new ConvertorBuilder(xmlNode, xnpmgr, activityEntity);
            return builder;
        }
    }
}
