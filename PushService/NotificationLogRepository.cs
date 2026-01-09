using Common;
using Dapper;
using Npgsql;

namespace PushService;

public class NotificationLogRepository
{
    private readonly IConfiguration _configuration;

    public NotificationLogRepository(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private NpgsqlConnection GetConnection()
    {
        var connStr = _configuration.GetConnectionString("DefaultConnection");
        return new NpgsqlConnection(connStr);
    }

    public async Task SaveAsync(NotificationMessage message, string status, string? error = null)
    {
        const string sql = @"
            INSERT INTO notification_logs (type, recipient, subject, message, status, error_message, created_at)
            VALUES (@Type, @Recipient, @Subject, @Message, @Status, @ErrorMessage, @CreatedAt);";

        var parameters = new
        {
            Type = message.Type.ToString(),
            message.Recipient,
            message.Subject,
            message.Message,
            Status = status,
            ErrorMessage = error,
            CreatedAt = DateTime.UtcNow
        };

        await using var connection = GetConnection();
        await connection.ExecuteAsync(sql, parameters);
    }
}