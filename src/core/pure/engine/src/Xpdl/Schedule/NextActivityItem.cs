using System;
using System.Collections.Generic;
using saiflow.engine.Xpdl.Entity;


namespace saiflow.engine.Xpdl.Schedule
{
    /// <summary>
    /// 单一下一步节点(不包含子节点）
    /// </summary>
    public class NextActivityItem : NextActivityComponent
    {
        #region Constructor
        public NextActivityItem(string name, 
            Transition transition,
            Activity activity)
        {
            base.Name = name;
            base.Transition = transition;
            base.Activity = activity;
        }
        #endregion

        /// <summary>
        /// Get enumerator
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<NextActivityComponent> GetEnumerator()
        {
            return null;
        }
    }
}
