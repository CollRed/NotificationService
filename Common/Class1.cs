using System.Text.Json.Serialization;

namespace Common
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum NotificationType
    {
        Email,
        Sms,
        Push
    }

    public class NotificationMessage
    {
        public NotificationType Type { get; set; }
        public string Recipient { get; set; }
        public string Subject { get; set; }  
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}