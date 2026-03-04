using ChatApp.Domain.Entities;

namespace ChatApp.Domain.Interfaces;

public interface IChatRoomRepository
{
    Task<ChatRoom?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatRoom>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ChatRoom chatRoom, CancellationToken cancellationToken = default);
    void Update(ChatRoom chatRoom);
}

public interface IMessageRepository
{
    Task<IEnumerable<Message>> GetLastMessagesAsync(Guid chatRoomId, int count = 50, CancellationToken cancellationToken = default);
    Task AddAsync(Message message, CancellationToken cancellationToken = default);
}

public interface IOutboxMessageRepository
{
    Task AddAsync(string type, string content, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
