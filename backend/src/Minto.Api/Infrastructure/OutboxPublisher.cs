using Microsoft.EntityFrameworkCore;
using Minto.Api.Data;
using RabbitMQ.Client;
using System.Text;

namespace Minto.Api.Infrastructure;

/// <summary>Publishes durable integration events from the database outbox.</summary>
public sealed class OutboxPublisher(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<OutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = configuration["RabbitMq:Host"];
        if (string.IsNullOrWhiteSpace(host)) { logger.LogInformation("RabbitMQ outbox publisher is disabled because RabbitMq:Host is not configured."); return; }
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await PublishBatchAsync(host, stoppingToken); }
            catch (Exception exception) { logger.LogWarning(exception, "RabbitMQ outbox publish attempt failed; retrying."); await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
        }
    }

    private async Task PublishBatchAsync(string host, CancellationToken ct)
    {
        var factory = new ConnectionFactory { HostName = host, UserName = configuration["RabbitMq:Username"] ?? "guest", Password = configuration["RabbitMq:Password"] ?? "guest" };
        using var connection = factory.CreateConnection(); using var channel = connection.CreateModel();
        channel.ExchangeDeclare("minto.events", ExchangeType.Topic, durable: true);
        using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MintoDbContext>();
        var events = await db.Outbox.Where(message => !message.Published).OrderBy(message => message.OccurredAt).Take(50).ToListAsync(ct);
        foreach (var message in events)
        {
            var body = Encoding.UTF8.GetBytes(message.Payload); channel.BasicPublish("minto.events", message.EventType, null, body);
            message.Published = true;
        }
        if (events.Count > 0) await db.SaveChangesAsync(ct); else await Task.Delay(TimeSpan.FromSeconds(2), ct);
    }
}
