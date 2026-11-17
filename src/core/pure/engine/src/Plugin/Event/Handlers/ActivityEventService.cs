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
    /// Activity Event Service
    /// 活动事件服务类
    /// </summary>
    public class ActivityEventService : EventServiceBase, IEventService
    {
/// <summary>
        /// Constructor
        /// </summary>
        /// <param name="kit"></param>
        /// <param name="context"></param>
        public ActivityEventService(SQLBuilder kit, EventContext context, SailClient client) : base(kit, context, client)
        {

        }

        /// <summary>
        /// Get Instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public override T GetInstance<T>(string id)
        {
            var aim = Sail.ManagerFactory.Activities;
            var entity = aim.GetById(Kit.useSQL(), id);
            return entity as T;
        }
    }
}
