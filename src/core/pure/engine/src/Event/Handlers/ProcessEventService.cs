using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Common;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Event;

namespace saiflow.engine.Event.Handlers
{
    /// <summary>
    /// Process Event Service
    /// 流程事件服务类
    /// </summary>
    public class ProcessEventService : EventServiceBase, IEventService
    {
        #region Property and Constructor
        public ProcessEventService(SQLBuilder kit, EventContext context) 
            : base(kit, context)
        {
            
        }
        #endregion

        /// <summary>
        /// Get Instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public override T GetInstance<T>(int id)
        {
            var pim = new ProcessInstanceManager();
            var kit = SailSqlAccess.Sibling(Kit);
            var entity = pim.GetById(kit, id);
            return entity as T;
        }
    }
}
