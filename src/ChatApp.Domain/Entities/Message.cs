using ChatApp.Domain.Exceptions;

namespace ChatApp.Domain.Entities;

public class Message : BaseEntity
{
    public string Content { get; private set; } = string.Empty;
    public string? UserId { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public Guid ChatRoomId { get; private set; }
    public bool IsBot { get; private set; }

    private Message() { }

    public static Message Create(string content, string userId, string username, Guid chatRoomId)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Message content cannot be empty.");

        return new Message
        {
            Content = content.Trim(),
            UserId = userId,
            Username = username,
            ChatRoomId = chatRoomId,
            IsBot = false
        };
    }

    public static Message CreateBotMessage(string content, string botName, Guid chatRoomId)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Bot message content cannot be empty.");

        return new Message
        {
            Content = content.Trim(),
            UserId = null,
            Username = botName,
            ChatRoomId = chatRoomId,
            IsBot = true
        };
    }
}
