using System.Net.Http;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using SmsService.Data;

public class SmsConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsConsumerService> _logger;
    private readonly NotificationLogRepository _repository;
    private readonly HttpClient _httpClient;

    private const string Topic = "notifications-sms";
    private const int MaxRetryAttempts = 3;
    private const string SmsApiUrl = "https://textbelt.com/text";
    private const string SmsApiKey = "textbelt";

    // 🔥 PROMETHEUS METRICS
    private static readonly Counter SmsSentCounter = Metrics.CreateCounter(
        "sms_sent_total",
        "Количество успешно отправленных SMS"
    );

    private static readonly Counter SmsFailedCounter = Metrics.CreateCounter(
        "sms_failed_total",
        "Количество неудачных отправок SMS"
    );

    private static readonly Histogram SmsSendDuration = Metrics.CreateHistogram(
        "sms_send_duration_seconds",
        "Время отправки SMS в секундах"
    );

    public SmsConsumerService(
        IConfiguration configuration,
        ILogger<SmsConsumerService> logger,
        NotificationLogRepository repository)
    {
        _configuration = configuration;
        _logger = logger;
        _repository = repository;
        _httpClient = new HttpClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "kafka:9092",
            GroupId = "sms-service-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        consumer.Subscribe(Topic);

        _logger.LogInformation("📲 SmsConsumerService запущен и слушает Kafka...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var message = JsonSerializer.Deserialize<NotificationMessage>(result.Message.Value);

                if (message != null)
                    await ProcessWithRetry(message);
                else
                    _logger.LogWarning("⚠️ Не удалось десериализовать сообщение");
            }
            catch (ConsumeException ex)
            {
                _logger.LogError($"❌ Kafka error: {ex.Error.Reason}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Общая ошибка SmsConsumerService");
            }
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
                _logger.LogInformation($"SMS -> {message.Recipient} | Попытка #{attempt}");

                using (SmsSendDuration.NewTimer()) // ⏱️ замер времени
                {
                    var response = await SendSmsAsync(message);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    bool success = response.IsSuccessStatusCode;
                    string status = success ? "SUCCESS" : "FAILED";

                    _logger.LogInformation($"Ответ Textbelt: {responseBody}");

                    if (success)
                        SmsSentCounter.Inc();
                    else
                        SmsFailedCounter.Inc();

                    await _repository.SaveAsync(message, status, responseBody);
                    return;
                }
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogWarning($"Попытка #{attempt} неудачна: {ex.Message}");
                await Task.Delay(1000);
            }
        }

        _logger.LogError($"SMS НЕ ОТПРАВЛЕНО после {MaxRetryAttempts} попыток -> {message.Recipient}");
        SmsFailedCounter.Inc();
        await _repository.SaveAsync(message, "FAILED", lastError?.Message);
    }

    private Task<HttpResponseMessage> SendSmsAsync(NotificationMessage message)
    {
        var payload = new
        {
            phone = message.Recipient,
            message = message.Message,
            key = SmsApiKey
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        return _httpClient.PostAsync(SmsApiUrl, content);
    }
}
