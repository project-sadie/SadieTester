using System.Text;
using RabbitMQ.Client;

namespace SadieTester
{
    public class MessageQueueWrapper : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public MessageQueueWrapper(IConnectionFactory connectionFactory)
        {
            _connection = connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        private void QueueDeclare(string queueName, bool durable = true)
        {
            _channel.QueueDeclare(
                queue: queueName,
                durable: durable,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        public void BasicPublish(string queueName, string message, string exchange = "")
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(exchange, queueName, null, bytes);
        }

        public BasicGetResult? BasicGet(string queueName, bool autoAck = false)
        {
            try
            {
                QueueDeclare(queueName);
                return _channel.BasicGet(queue: queueName, autoAck: autoAck);
            }
            catch (TimeoutException)
            {
                return null;
            }
        }

        public void BasicAck(ulong deliveryTag)
        {
            _channel.BasicAck(deliveryTag, false);
        }

        public void BasicNack(ulong deliveryTag, bool requeue)
        {
            _channel.BasicNack(deliveryTag, false, requeue);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}