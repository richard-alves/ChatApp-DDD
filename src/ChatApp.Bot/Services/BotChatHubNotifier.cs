using ChatApp.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatApp.Bot.Services;

/// <summary>
/// No-op hub notifier for the Bot service.
/// The bot uses the database directly; real-time notifications are handled by the API.
/// </summary>
public class BotChatHubNotifier(ILogger<BotChatHubNotifier> logger) : IChatHubNotifier
{
    public Task NotifyMessageAsync(Guid chatRoomId, MessageNotification notification, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Bot posted message to room {ChatRoomId}: {Message}", chatRoomId, notification.Content);
        return Task.CompletedTask;
    }
}
