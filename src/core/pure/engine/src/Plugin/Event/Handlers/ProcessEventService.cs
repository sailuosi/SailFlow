using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using sailflow.data;
using mooSQL.data;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Manager;
using sailflow.engine.Event;

using sailflow.engine.Client;
namespace sailflow.engine.Event
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
        public override T GetInstance<T>(string id)
        {
            var pim = Sail.ProcessInstances;
            var kit = Kit.useSQL();
            var entity = pim.GetById(kit, id);
            return entity as T;
        }
    }
}
