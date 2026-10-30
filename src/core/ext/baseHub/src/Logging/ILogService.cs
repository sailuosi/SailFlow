using System;
using System.Collections.Generic;
using System.Text;

namespace saiflow.hub.Logging
{
    /// <summary>
    /// Log Service Interface
    /// 日志服务接口
    /// </summary>
    public interface ILogService
    {
        void Record(object logEntity);
    }
}
