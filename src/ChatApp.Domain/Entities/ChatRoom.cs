using ChatApp.Domain.Events;
using ChatApp.Domain.Exceptions;

namespace ChatApp.Domain.Entities;

public class ChatRoom : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

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
}
