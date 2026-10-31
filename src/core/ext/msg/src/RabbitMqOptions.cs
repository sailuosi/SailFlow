namespace saiflow.msg
{
    /// <summary>
    /// RabbitMQ 连接选项（懒连接；首次 Publish/Subscribe 时建连）。
    /// </summary>
    public sealed class RabbitMqOptions
    {
        public string HostName { get; set; } = "localhost";

        public int Port { get; set; } = 5672;

        public string UserName { get; set; } = "guest";

        public string Password { get; set; } = "guest";

        public string VirtualHost { get; set; } = "/";
    }
}
