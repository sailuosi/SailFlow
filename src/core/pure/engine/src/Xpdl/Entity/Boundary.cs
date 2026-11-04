using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using saiflow.engine.Common;

namespace saiflow.engine.Xpdl
{
    /// <summary>
    /// Boundary
    /// </summary>
    public class Boundary
    {
        public EventTriggerEnum EventTriggerType { get; set; }
        public string Expression { get; set; }
    }
}
