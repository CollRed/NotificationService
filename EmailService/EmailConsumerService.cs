using System.Text.Json;
using Common;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit.Text;
using Prometheus; 

namespace EmailService;

public class EmailConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailConsumerService> _logger;
    private readonly NotificationLogRepository _repository;

    private const string Topic = "notifications-email";
    private const int MaxRetryAttempts = 3;

    // Метрики
    private static readonly Counter EmailSentCounter = Metrics.CreateCounter(
        "email_sent_total", "Общее число успешно отправленных email");

    private static readonly Counter EmailFailedCounter = Metrics.CreateCounter(
        "email_failed_total", "Общее число неудачных попыток отправки email");

    private static readonly Histogram EmailSendDuration = Metrics.CreateHistogram(
        "email_send_duration_seconds", "Время, затраченное на отправку email в секундах");

    public EmailConsumerService(
        IConfiguration configuration,
        ILogger<EmailConsumerService> logger,
        NotificationLogRepository repository)
    {
        _configuration = configuration;
        _logger = logger;
        _repository = repository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "kafka:9092",
                GroupId = "email-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(Topic);

            _logger.LogInformation("📥 EmailConsumerService запущен и слушает Kafka...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    _logger.LogInformation($"Получено сообщение: {result.Message.Value}");

                    var message = JsonSerializer.Deserialize<NotificationMessage>(result.Message.Value);

                    if (message != null)
                        await ProcessWithRetry(message);
                    else
                        _logger.LogWarning("Не удалось десериализовать сообщение");
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError($"Kafka ошибка: {ex.Error.Reason}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка внутри цикла Consume()");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Сервис упал на старте и не запустился");
        }
    }

    private async Task ProcessWithRetry(NotificationMessage message)
    {
        int attempt = 0;
        Exception? lastError = null;

        while (attempt < MaxRetryAttempts)
        {
            try
            {
                attempt++;
                _logger.LogInformation($"Email -> {message.Recipient}: {message.Subject} | Попытка #{attempt}");

                var smtpSection = _configuration.GetSection("Smtp");
                var email = smtpSection["Username"];
                var password = smtpSection["Password"];
                var host = smtpSection["Host"];
                var port = int.Parse(smtpSection["Port"] ?? "587");
                var from = smtpSection["From"];

                using (EmailSendDuration.NewTimer()) // Засекаем время отправки
                {
                    var mimeMessage = new MimeMessage();
                    mimeMessage.From.Add(MailboxAddress.Parse(from));
                    mimeMessage.To.Add(MailboxAddress.Parse(message.Recipient));
                    mimeMessage.Subject = message.Subject;
                    mimeMessage.Body = new TextPart(TextFormat.Plain)
                    {
                        Text = message.Message
                    };

                    using var client = new SmtpClient();
                    await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(email, password);
                    await client.SendAsync(mimeMessage);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation("Email успешно отправлен!");
                EmailSentCounter.Inc();
                await _repository.SaveAsync(message, "SUCCESS");
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogWarning($"Попытка #{attempt} неудачна: {ex.Message}");
                await Task.Delay(1000);
            }
        }

        _logger.LogError($"EMAIL НЕ ОТПРАВЛЕН после {MaxRetryAttempts} попыток: {message.Recipient}");
        EmailFailedCounter.Inc();
        await _repository.SaveAsync(message, "FAILED", lastError?.Message);
    }
}
