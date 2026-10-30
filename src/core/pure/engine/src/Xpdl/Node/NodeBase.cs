using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Utility;
using saiflow.data;
using saiflow.engine.Common;
using saiflow.engine.Business.Entity;
using saiflow.engine.Xpdl.Entity;

namespace saiflow.engine.Xpdl.Node
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
