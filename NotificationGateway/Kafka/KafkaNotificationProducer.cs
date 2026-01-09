using System.Text.Json;
using Common;
using Confluent.Kafka;

namespace NotificationGateway.Kafka
{
    public class KafkaNotificationProducer
    {
        private readonly IProducer<Null, string> _producer;

        public KafkaNotificationProducer(IConfiguration configuration)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092"
            };

            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task SendAsync(NotificationMessage message)
        {
            var topic = GetTopicForNotificationType(message.Type);
            var json = JsonSerializer.Serialize(message);

            Console.WriteLine($"[Kafka] Отправлено уведомление в топик `{topic}`: {json}");

            await _producer.ProduceAsync(topic, new Message<Null, string> { Value = json });
        }

        private static string GetTopicForNotificationType(NotificationType type) => type switch
        {
            NotificationType.Email => "notifications-email",
            NotificationType.Sms => "notifications-sms",
            NotificationType.Push => "notifications-push",
            _ => "notifications-unknown"
        };
    }
}