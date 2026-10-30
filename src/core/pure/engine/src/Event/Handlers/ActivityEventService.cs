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
        public ActivityEventService(SQLBuilder kit, EventContext context)
            : base(kit, context)
        {

        }

        /// <summary>
        /// Get Instance
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public override T GetInstance<T>(int id)
        {
            var aim = new ActivityInstanceManager();
            var entity = aim.GetById(SailSqlAccess.Sibling(Kit), id);
            return entity as T;
        }
    }
}
