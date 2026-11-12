using System;
using System.Collections.Generic;
using System.Text;
using mooSQL.data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using sailflow.hub.Essential;

namespace sailflow.msg
{
    /// <summary>
    /// RabbitMQ <see cref="IMessageQueueService"/> 实现。
    /// </summary>
    public sealed class RabbitMQService : IMessageQueueService
    {
        private readonly RabbitMQClientFactory _clientFactory;

        public Func<MessageEntity, MessageConsumedResult> ConsumeMessageFunction { get; set; }

        public Func<SignalEntity, List<SignalConsumedResult>> ConsumeSignalFunction { get; set; }

        /// <summary>宿主注入：() => sailClient.UseRunSQL()</summary>
        public Func<SQLBuilder> UseRunSql { get; set; }

        public RabbitMQService(RabbitMqOptions options = null)
            : this(new RabbitMQClientFactory(options))
        {
        }

        public RabbitMQService(RabbitMQClientFactory clientFactory)
        {
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        public void Publish(string topic, string line)
        {
            var channel = _clientFactory.CreatePublishChannel();
            channel.QueueDeclare(queue: topic,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(line);
            channel.BasicPublish(exchange: "",
                routingKey: topic,
                basicProperties: null,
                body: body);
        }

        public void Publish(MessageEntity message)
        {
            Publish(message.Topic, message.Line);
        }

        public void Subscribe(string topic, string jobName, string strTriggerType)
        {
            var channel = _clientFactory.CreateReceiveChannel();
            channel.QueueDeclare(queue: topic,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var line = Encoding.UTF8.GetString(body.ToArray());

                if (strTriggerType == "Message")
                {
                    var msgMediator = new MessageMediator(UseRunSql
                        ?? throw new InvalidOperationException("RabbitMQService.UseRunSql is not set."));
                    msgMediator.InvokeFromMessage(ConsumeMessageFunction, topic, line, jobName);
                }
                else if (strTriggerType == "Signal")
                {
                    var signalMediator = new SignalMediator(UseRunSql
                        ?? throw new InvalidOperationException("RabbitMQService.UseRunSql is not set."));
                    signalMediator.InvokeFromSignal(ConsumeSignalFunction, topic, line, jobName);
                }
                else
                {
                    throw new ApplicationException(
                        string.Format("There isnt a mediator to handle this trigger type:{0}", strTriggerType));
                }
            };
            channel.BasicConsume(queue: topic,
                autoAck: true,
                consumer: consumer);
        }

        public void Unsubscribe(string topic)
        {
            var channel = _clientFactory.CreateReceiveChannel();
            channel.QueueDelete(topic);
        }
    }
}
