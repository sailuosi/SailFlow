using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.engine.Common;

namespace sailflow.engine.Xpdl
{
    /// <summary>
    /// Condtion Detail on Transition
    /// </summary>
    public class ConditionDetail
    {
        public ConditionTypeEnum ConditionType
        {
            get;
            set;
        }

        public string ConditionText
        {
            get;
            set;
        }
    }
}
