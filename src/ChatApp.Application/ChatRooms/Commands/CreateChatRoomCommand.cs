using ChatApp.Application.Common;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ChatApp.Application.ChatRooms.Commands;

// --- Create ChatRoom ---
public record CreateChatRoomCommand(string Name, string Description = "") : IRequest<Result<ChatRoomDto>>;

public record ChatRoomDto(Guid Id, string Name, string Description, bool IsActive, DateTime CreatedAt);

public class CreateChatRoomCommandValidator : AbstractValidator<CreateChatRoomCommand>
{
    public CreateChatRoomCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateChatRoomCommandHandler(IChatRoomRepository chatRoomRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateChatRoomCommand, Result<ChatRoomDto>>
{
    public async Task<Result<ChatRoomDto>> Handle(CreateChatRoomCommand request, CancellationToken cancellationToken)
    {
        var room = ChatRoom.Create(request.Name, request.Description);
        await chatRoomRepository.AddAsync(room, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ChatRoomDto(room.Id, room.Name, room.Description, room.IsActive, room.CreatedAt);
        return Result<ChatRoomDto>.Success(dto);
    }
}
