using ChatApp.Application.ChatRooms.Commands;
using ChatApp.Application.Common;
using ChatApp.Domain.Interfaces;
using MediatR;

namespace ChatApp.Application.ChatRooms.Queries;

public record GetAllChatRoomsQuery : IRequest<Result<IEnumerable<ChatRoomDto>>>;

public class GetAllChatRoomsQueryHandler(IChatRoomRepository chatRoomRepository)
    : IRequestHandler<GetAllChatRoomsQuery, Result<IEnumerable<ChatRoomDto>>>
{
    public async Task<Result<IEnumerable<ChatRoomDto>>> Handle(GetAllChatRoomsQuery request, CancellationToken cancellationToken)
    {
        var rooms = await chatRoomRepository.GetAllActiveAsync(cancellationToken);
        var dtos = rooms.Select(r => new ChatRoomDto(r.Id, r.Name, r.Description, r.IsActive, r.CreatedAt));
        return Result<IEnumerable<ChatRoomDto>>.Success(dtos);
    }
}
