using ChatApp.Application.Common;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Exceptions;
using ChatApp.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ChatApp.Application.Messages.Commands;

public record PostBotMessageCommand(
    Guid ChatRoomId,
    string Content,
    string BotName = "StockBot") : IRequest<Result<MessageDto>>;

public class PostBotMessageCommandHandler(
    IChatRoomRepository chatRoomRepository,
    IMessageRepository messageRepository,
    IUnitOfWork unitOfWork,
    IChatHubNotifier hubNotifier,
    ILogger<PostBotMessageCommandHandler> logger)
    : IRequestHandler<PostBotMessageCommand, Result<MessageDto>>
{
    public async Task<Result<MessageDto>> Handle(PostBotMessageCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await chatRoomRepository.GetByIdAsync(request.ChatRoomId, cancellationToken);
        if (chatRoom is null)
        {
            logger.LogWarning("ChatRoom {ChatRoomId} not found for bot message.", request.ChatRoomId);
            return Result<MessageDto>.Failure($"ChatRoom {request.ChatRoomId} not found.");
        }

        var message = Message.CreateBotMessage(request.Content, request.BotName, request.ChatRoomId);
        await messageRepository.AddAsync(message, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var notification = new MessageNotification(
            message.Id, message.ChatRoomId, message.Content,
            message.Username, message.IsBot, message.CreatedAt);

        await hubNotifier.NotifyMessageAsync(request.ChatRoomId, notification, cancellationToken);

        return Result<MessageDto>.Success(new MessageDto(
            message.Id, message.ChatRoomId, message.Content,
            message.Username, message.IsBot, message.CreatedAt));
    }
}
