using ChatApp.Application.Common;
using ChatApp.Application.Messages.Commands;
using ChatApp.Domain.Interfaces;
using MediatR;

namespace ChatApp.Application.Messages.Queries;

public record GetMessagesQuery(Guid ChatRoomId, int Count = 50) : IRequest<Result<IEnumerable<MessageDto>>>;

public class GetMessagesQueryHandler(IMessageRepository messageRepository)
    : IRequestHandler<GetMessagesQuery, Result<IEnumerable<MessageDto>>>
{
    public async Task<Result<IEnumerable<MessageDto>>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var messages = await messageRepository.GetLastMessagesAsync(request.ChatRoomId, request.Count, cancellationToken);
        var dtos = messages.Select(m => new MessageDto(m.Id, m.ChatRoomId, m.Content, m.Username, m.IsBot, m.CreatedAt));
        return Result<IEnumerable<MessageDto>>.Success(dtos);
    }
}
