using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.engine.Utility;
using sailflow.data;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Xpdl
{
    /// <summary>
    /// Node base
    /// </summary>
    public abstract class NodeBase
    {
        #region Property and Constructor
        internal Activity Activity
        {
            get;
            set;
        }

        public ActivityInstanceEntity ActivityInstance
        {
            get;
            set;
        }
        
        public NodeBase(Activity currentActivity)
        {
            Activity = currentActivity;
        }
        #endregion
    }
}
