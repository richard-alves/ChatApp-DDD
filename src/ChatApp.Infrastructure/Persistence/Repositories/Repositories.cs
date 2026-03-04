using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Persistence.Repositories;

public class ChatRoomRepository(AppDbContext context) : IChatRoomRepository
{
    public async Task<ChatRoom?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.ChatRooms.FindAsync([id], cancellationToken);

    //public async Task<ChatRoom?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default)
    //    => await context.ChatRooms
    //        .Include(r => r.Messages)
    //        .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IEnumerable<ChatRoom>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await context.ChatRooms.Where(r => r.IsActive).ToListAsync(cancellationToken);

    public async Task AddAsync(ChatRoom chatRoom, CancellationToken cancellationToken = default)
        => await context.ChatRooms.AddAsync(chatRoom, cancellationToken);

    public void Update(ChatRoom chatRoom) => context.ChatRooms.Update(chatRoom);
}

public class MessageRepository(AppDbContext context) : IMessageRepository
{
    public async Task<IEnumerable<Message>> GetLastMessagesAsync(Guid chatRoomId, int count = 50, CancellationToken cancellationToken = default)
        => await context.Messages
            .Where(m => m.ChatRoomId == chatRoomId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Message message, CancellationToken cancellationToken = default)
        => await context.Messages.AddAsync(message, cancellationToken);
}
