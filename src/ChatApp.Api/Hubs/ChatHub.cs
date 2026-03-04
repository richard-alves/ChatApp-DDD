using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public async Task JoinRoom(string chatRoomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, chatRoomId);
        await Clients.Group(chatRoomId).SendAsync("UserJoined", new
        {
            Username = Context.User?.FindFirst("username")?.Value ?? "Unknown",
            ChatRoomId = chatRoomId,
            Timestamp = DateTime.UtcNow
        });
    }

    public async Task LeaveRoom(string chatRoomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatRoomId);
        await Clients.Group(chatRoomId).SendAsync("UserLeft", new
        {
            Username = Context.User?.FindFirst("username")?.Value ?? "Unknown",
            ChatRoomId = chatRoomId,
            Timestamp = DateTime.UtcNow
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
