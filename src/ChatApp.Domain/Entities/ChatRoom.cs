using ChatApp.Domain.Events;
using ChatApp.Domain.Exceptions;

namespace ChatApp.Domain.Entities;

public class ChatRoom : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    //private readonly List<Message> _messages = [];
    //public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    private ChatRoom() { }

    public static ChatRoom Create(string name, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("ChatRoom name cannot be empty.");

        var room = new ChatRoom
        {
            Name = name.Trim(),
            Description = description.Trim()
        };

        room.AddDomainEvent(new ChatRoomCreatedEvent(room.Id, room.Name));
        return room;
    }

    //public Message PostMessage(string content, string userId, string username)
    //{
    //    if (!IsActive)
    //        throw new DomainException("Cannot post to an inactive chatroom.");

    //    var message = Message.Create(content, userId, username, Id);
    //    _messages.Add(message);
    //    AddDomainEvent(new MessagePostedEvent(message.Id, Id, userId, username, content));
    //    return message;
    //}

    //public Message PostBotMessage(string content, string botName = "StockBot")
    //{
    //    if (!IsActive)
    //        throw new DomainException("Cannot post to an inactive chatroom.");

    //    var message = Message.CreateBotMessage(content, botName, Id);
    //    _messages.Add(message);
    //    AddDomainEvent(new MessagePostedEvent(message.Id, Id, null, botName, content));
    //    return message;
    //}

    //public IEnumerable<Message> GetLastMessages(int count = 50)
    //    => _messages.OrderBy(m => m.CreatedAt).TakeLast(count);
}
