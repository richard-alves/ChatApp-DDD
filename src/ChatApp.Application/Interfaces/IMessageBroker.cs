namespace ChatApp.Application.Interfaces;

public interface IMessageBroker
{
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default) where T : class;
}

public interface IChatHubNotifier
{
    Task NotifyMessageAsync(Guid chatRoomId, MessageNotification notification, CancellationToken cancellationToken = default);
}

public record MessageNotification(
    Guid MessageId,
    Guid ChatRoomId,
    string Content,
    string Username,
    bool IsBot,
    DateTime CreatedAt);
