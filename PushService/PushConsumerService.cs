using System.Text.Json;
using Common;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace PushService;

public class PushConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushConsumerService> _logger;
    private readonly NotificationLogRepository _repository;

    private static readonly Counter PushSentCounter = Metrics
        .CreateCounter("push_sent_total", "Общее количество отправленных PUSH-уведомлений");

    private static readonly Counter PushFailedCounter = Metrics
        .CreateCounter("push_failed_total", "Количество неудачных PUSH-уведомлений");

    private const string Topic = "notifications-push";
    private const int MaxRetryAttempts = 3;

    public PushConsumerService(
        IConfiguration configuration,
        ILogger<PushConsumerService> logger,
        NotificationLogRepository repository)
    {
        _configuration = configuration;
        _logger = logger;
        _repository = repository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "kafka:9092",
            GroupId = "push-service-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(Topic);

        _logger.LogInformation("PushConsumerService запущен и слушает Kafka...");

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
                _logger.LogError(ex, "Общая ошибка PushConsumerService");
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
                _logger.LogInformation($"Push -> {message.Recipient}: {message.Message} | Попытка #{attempt}");

                // Здесь можно реализовать Пуш уведомления на какой либо сервис, мы же просто делаем имитацию

                PushSentCounter.Inc(); 
                await _repository.SaveAsync(message, "SUCCESS");
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogWarning($"❌ Попытка #{attempt} неудачна: {ex.Message}");
                await Task.Delay(1000);
            }
        }

        PushFailedCounter.Inc();
        _logger.LogError($"PUSH НЕ ОТПРАВЛЕН после {MaxRetryAttempts} попыток: {message.Recipient}");
        await _repository.SaveAsync(message, "FAILED", lastError?.Message);
    }
}
