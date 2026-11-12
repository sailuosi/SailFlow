using System;
using System.Collections.Generic;
using System.Text;
using sailflow.hub.Essential;

namespace sailflow.hub.Essential
{
    /// <summary>
    /// Message Queue Service Interface
    /// 消息队列服务接口
    /// </summary>
    public interface IMessageQueueService
    {
        void Publish(string topic, string line);
        void Publish(MessageEntity message);
        void Subscribe(string topic, string jobName, string triggerType);
        void Unsubscribe(string topic);
    }
}
