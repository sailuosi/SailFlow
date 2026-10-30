using saiflow.engine.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace saiflow.engine.External
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
