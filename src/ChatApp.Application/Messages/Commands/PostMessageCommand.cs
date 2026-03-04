using ChatApp.Application.Common;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Exceptions;
using ChatApp.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ChatApp.Application.Messages.Commands;

public record PostMessageCommand(
    Guid ChatRoomId,
    string Content,
    string UserId,
    string Username) : IRequest<Result<MessageDto>>;

public record MessageDto(
    Guid Id,
    Guid ChatRoomId,
    string Content,
    string Username,
    bool IsBot,
    DateTime CreatedAt);

public class PostMessageCommandValidator : AbstractValidator<PostMessageCommand>
{
    public PostMessageCommandValidator()
    {
        RuleFor(x => x.ChatRoomId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Username).NotEmpty();
    }
}

public class PostMessageCommandHandler(
    IChatRoomRepository chatRoomRepository,
    IMessageRepository messageRepository,
    IUnitOfWork unitOfWork,
    IOutboxMessageRepository outboxMessageRepository,
    IChatHubNotifier hubNotifier,
    ILogger<PostMessageCommandHandler> logger)
    : IRequestHandler<PostMessageCommand, Result<MessageDto>>
{
    private const string StockCommandPrefix = "/stock=";

    public async Task<Result<MessageDto>> Handle(PostMessageCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await chatRoomRepository.GetByIdAsync(request.ChatRoomId, cancellationToken);
        if (chatRoom is null)
            return Result<MessageDto>.Failure($"ChatRoom {request.ChatRoomId} not found.");

        // Check if it's a stock command - don't persist it
        if (request.Content.StartsWith(StockCommandPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var stockCode = request.Content[StockCommandPrefix.Length..].Trim();
            logger.LogInformation("Stock command received for code: {StockCode} in room {ChatRoomId}", stockCode, request.ChatRoomId);

            //await messageBroker.PublishAsync(
            //    "stock.exchange",
            //    "stock.query",
            //    new StockQueryMessage(stockCode, request.ChatRoomId),
            //    cancellationToken);

            await outboxMessageRepository.AddAsync(
                "StockQuery",
                JsonConvert.SerializeObject(new StockQueryMessage(stockCode, request.ChatRoomId)),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var notification = new MessageNotification(
                Guid.Empty,
                request.ChatRoomId,
                $"Fetching quote for {stockCode.ToUpper()}, please wait...",
                "StockBot",
                true,
                DateTime.UtcNow);

            await hubNotifier.NotifyMessageAsync(request.ChatRoomId, notification, cancellationToken);


            return Result<MessageDto>.Success(new MessageDto(Guid.Empty, request.ChatRoomId, request.Content, request.Username, false, DateTime.UtcNow));
        }

        try
        {
            //var message = chatRoom.PostMessage(request.Content, request.UserId, request.Username);
            var message = Message.Create(request.Content, request.UserId, request.Username, request.ChatRoomId);
            await messageRepository.AddAsync(message, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var notification = new MessageNotification(
                message.Id, message.ChatRoomId, message.Content,
                message.Username, message.IsBot, message.CreatedAt);

            await hubNotifier.NotifyMessageAsync(request.ChatRoomId, notification, cancellationToken);

            var dto = new MessageDto(message.Id, message.ChatRoomId, message.Content, message.Username, message.IsBot, message.CreatedAt);
            return Result<MessageDto>.Success(dto);
        }
        catch (DomainException ex)
        {
            return Result<MessageDto>.Failure(ex.Message);
        }catch(Exception ex)
        {
            return Result<MessageDto>.Failure(ex.Message);
        }
    }
}

public record StockQueryMessage(string StockCode, Guid ChatRoomId);
