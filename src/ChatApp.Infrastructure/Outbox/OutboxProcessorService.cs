using ChatApp.Application.Interfaces;
using ChatApp.Application.Messages.Commands;
using ChatApp.Infrastructure.Messaging;
using ChatApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ChatApp.Infrastructure.Outbox;

public class OutboxProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox Processor started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OutboxProcessorService.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        
        }
    }

    private async Task ProcessOutboxMessagesAsyncWithoutOutbox(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 3)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        logger.LogInformation("Processing {Count} outbox messages.", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                // Mark as processed - event was already dispatched via mediator in SaveChanges
                message.ProcessedAt = DateTime.UtcNow;
                logger.LogDebug("Outbox message {Id} of type {Type} marked as processed.", message.Id, message.Type);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                logger.LogWarning(ex, "Failed to process outbox message {Id}.", message.Id);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var messageBroker = scope.ServiceProvider.GetRequiredService<IMessageBroker>();
        var hubNotifier = scope.ServiceProvider.GetRequiredService<IChatHubNotifier>();


        // Pega só os que ainda não foram publicados no RabbitMQ
        var messages = await context.OutboxMessages
            .Where(m => m.PublishedAt == null && m.RetryCount < 3)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            try
            {
                if (message.Type == "StockQuery")
                {
                    var stockQuery = JsonConvert.DeserializeObject<StockQueryMessage>(message.Content)!;

                    await messageBroker.PublishAsync(
                        "stock.exchange",
                        "stock.query",
                        stockQuery,
                        cancellationToken);

                    message.PublishedAt = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                logger.LogWarning(ex, "Failed to publish outbox message {Id}.", message.Id);

                if (message.RetryCount >= 3)
                {
                    var stockQuery = JsonConvert.DeserializeObject<StockQueryMessage>(message.Content)!;
                    await hubNotifier.NotifyMessageAsync(stockQuery.ChatRoomId, new MessageNotification(
                        Guid.Empty,
                        stockQuery.ChatRoomId,
                        $"Could not process stock quote. Please try again later.",
                        "StockBot",
                        true,
                        DateTime.UtcNow), cancellationToken);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
