using ChatApp.Domain.Entities;

namespace ChatApp.Domain.Events;

public sealed record ChatRoomCreatedEvent(Guid ChatRoomId, string Name) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public sealed record MessagePostedEvent(
    Guid MessageId,
    Guid ChatRoomId,
    string? UserId,
    string Username,
    string Content) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
