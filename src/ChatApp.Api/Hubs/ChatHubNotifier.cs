using ChatApp.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Hubs;

public class ChatHubNotifier(IHubContext<ChatHub> hubContext) : IChatHubNotifier
{
    public async Task NotifyMessageAsync(Guid chatRoomId, MessageNotification notification, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(chatRoomId.ToString())
            .SendAsync("ReceiveMessage", notification, cancellationToken);
    }
}
