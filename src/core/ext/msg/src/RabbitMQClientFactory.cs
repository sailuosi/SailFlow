using System;
using RabbitMQ.Client;

namespace sailflow.msg
{
    /// <summary>
    /// RabbitMQ 通道工厂（懒连接，按实例配置）。
    /// </summary>
    public sealed class RabbitMQClientFactory
    {
        private readonly RabbitMqOptions _options;
        private readonly object _gate = new();
        private ConnectionFactory _connectionFactory;
        private IConnection _publishConnection;
        private IModel _publishChannel;
        private IConnection _receiveConnection;
        private IModel _receiveChannel;

        public RabbitMQClientFactory(RabbitMqOptions options = null)
        {
            _options = options ?? new RabbitMqOptions();
        }

        public IModel CreatePublishChannel()
        {
            EnsureFactory();
            lock (_gate)
            {
                if (_publishConnection == null || !_publishConnection.IsOpen)
                    _publishConnection = _connectionFactory.CreateConnection();
                if (_publishChannel == null || !_publishChannel.IsOpen)
                    _publishChannel = _publishConnection.CreateModel();
                return _publishChannel;
            }
        }

        public IModel CreateReceiveChannel()
        {
            EnsureFactory();
            lock (_gate)
            {
                if (_receiveConnection == null || !_receiveConnection.IsOpen)
                    _receiveConnection = _connectionFactory.CreateConnection();
                if (_receiveChannel == null || !_receiveChannel.IsOpen)
                    _receiveChannel = _receiveConnection.CreateModel();
                return _receiveChannel;
            }
        }

        /// <summary>兼容旧拼写。</summary>
        public IModel CreateRecieveChannel() => CreateReceiveChannel();

        private void EnsureFactory()
        {
            if (_connectionFactory != null)
                return;

            lock (_gate)
            {
                if (_connectionFactory != null)
                    return;

                _connectionFactory = new ConnectionFactory
                {
                    HostName = _options.HostName ?? "localhost",
                    Port = _options.Port > 0 ? _options.Port : 5672,
                    UserName = string.IsNullOrEmpty(_options.UserName) ? "guest" : _options.UserName,
                    Password = string.IsNullOrEmpty(_options.Password) ? "guest" : _options.Password,
                    VirtualHost = string.IsNullOrEmpty(_options.VirtualHost) ? "/" : _options.VirtualHost,
                };
            }
        }
    }
}
