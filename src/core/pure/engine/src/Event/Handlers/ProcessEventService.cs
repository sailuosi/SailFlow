using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Common;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Event;

using saiflow.engine.Client;
namespace saiflow.engine.Event
{
    /// <summary>
    /// Process Event Service
    /// 流程事件服务类
    /// </summary>
    public class ProcessEventService : EventServiceBase, IEventService
    {
#region Property and Constructor
        public ProcessEventService(SQLBuilder kit, EventContext context, SailClient client) : base(kit, context, client)
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
            var pim = Sail.ProcessInstances;
            var kit = SailSqlAccess.Sibling(Kit);
            var entity = pim.GetById(kit, id);
            return entity as T;
        }
    }
}
