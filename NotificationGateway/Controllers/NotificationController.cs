using Microsoft.AspNetCore.Mvc;
using Common;
using NotificationGateway.Kafka;

namespace NotificationGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly KafkaNotificationProducer _producer;

        public NotificationController(KafkaNotificationProducer producer)
        {
            _producer = producer;
        }

        [HttpPost]
        public async Task<IActionResult> SendNotification([FromBody] NotificationMessage notification)
        {
            if (notification == null)
                return BadRequest("notification == null");

            await _producer.SendAsync(notification);

            return Ok(new
            {
                Status = "Published to Kafka",
                notification.Type,
                notification.Recipient
            });
        }
    }
}