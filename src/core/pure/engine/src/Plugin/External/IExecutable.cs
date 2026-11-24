using sailflow.events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sailflow.plugin
{
    /// <summary>
    /// Externally executable interface
    /// 可外部执行接口
    /// </summary>
    public interface IExecutable
    {
        void Executable(IEventService eventService);
    }
}
