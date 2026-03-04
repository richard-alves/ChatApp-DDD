using ChatApp.Application.Interfaces;
using ChatApp.Application.Messages.Commands;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using ChatApp.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ChatApp.Tests.Application;

public class PostMessageCommandHandlerTests
{
    private readonly Mock<IChatRoomRepository> _chatRoomRepoMock = new();
    private readonly Mock<IMessageRepository> _messageRepositoryMock = new();
    private readonly Mock<IOutboxMessageRepository> _outboxMessageRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMessageBroker> _messageBrokerMock = new();
    private readonly Mock<IChatHubNotifier> _hubNotifierMock = new();
    private readonly Mock<ILogger<PostMessageCommandHandler>> _loggerMock = new();

    private PostMessageCommandHandler CreateHandler() => new(
        _chatRoomRepoMock.Object,
        _messageRepositoryMock.Object,
        _unitOfWorkMock.Object,
        _outboxMessageRepositoryMock.Object,
        _hubNotifierMock.Object,
        _loggerMock.Object);

    [Fact]
    public async Task Handle_WithValidMessage_ShouldPostAndNotify()
    {
        // Arrange
        var chatRoom = ChatRoom.Create("General");
        _chatRoomRepoMock.Setup(r => r.GetByIdAsync(chatRoom.Id, default)).ReturnsAsync(chatRoom);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new PostMessageCommand(chatRoom.Id, "Hello", "user-1", "Alice");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Be("Hello");
        _hubNotifierMock.Verify(h => h.NotifyMessageAsync(chatRoom.Id, It.IsAny<MessageNotification>(), default), Times.Once);
    }


    [Fact]
    public async Task Handle_WithStockCommand_ShouldSaveToOutbox()
    {
        // Arrange
        var chatRoom = ChatRoom.Create("General");
        _chatRoomRepoMock.Setup(r => r.GetByIdAsync(chatRoom.Id, default)).ReturnsAsync(chatRoom);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new PostMessageCommand(chatRoom.Id, "/stock=aapl.us", "user-1", "Alice");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verifica que salvou no Outbox
        _outboxMessageRepositoryMock.Verify(o => o.AddAsync(
            "StockQuery",
            It.Is<string>(s => s.Contains("aapl.us")),
            default), Times.Once);

        // Verifica que NÃO chamou o broker diretamente
        _messageBrokerMock.Verify(b => b.PublishAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<StockQueryMessage>(), default), Times.Never);

        // Verifica que salvou no banco
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonexistentChatRoom_ShouldReturnFailure()
    {
        // Arrange
        _chatRoomRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((ChatRoom?)null);

        var command = new PostMessageCommand(Guid.NewGuid(), "Hello", "user-1", "Alice");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_StockCommand_CaseInsensitive_ShouldSaveToOutbox()
    {
        // Arrange
        var chatRoom = ChatRoom.Create("General");
        _chatRoomRepoMock.Setup(r => r.GetByIdAsync(chatRoom.Id, default)).ReturnsAsync(chatRoom);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new PostMessageCommand(chatRoom.Id, "/STOCK=TSLA.US", "user-1", "Alice");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _outboxMessageRepositoryMock.Verify(o => o.AddAsync(
            "StockQuery",
            It.Is<string>(s => s.Contains("TSLA.US")),
            default), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidMessage_ShouldNotifyHub()
    {
        var chatRoom = ChatRoom.Create("General");
        _chatRoomRepoMock.Setup(r => r.GetByIdAsync(chatRoom.Id, default)).ReturnsAsync(chatRoom);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new PostMessageCommand(chatRoom.Id, "Hello", "user-1", "Alice");
        var handler = CreateHandler();

        await handler.Handle(command, default);

        _hubNotifierMock.Verify(h => h.NotifyMessageAsync(
            chatRoom.Id, It.IsAny<MessageNotification>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_WithStockCommand_ShouldNotifyWithPleaseWait()
    {
        var chatRoom = ChatRoom.Create("General");
        _chatRoomRepoMock.Setup(r => r.GetByIdAsync(chatRoom.Id, default)).ReturnsAsync(chatRoom);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new PostMessageCommand(chatRoom.Id, "/stock=aapl.us", "user-1", "Alice");
        await CreateHandler().Handle(command, default);

        _hubNotifierMock.Verify(h => h.NotifyMessageAsync(
            chatRoom.Id,
            It.Is<MessageNotification>(n => n.Content.Contains("please wait")),
            default), Times.Once);
    }

    [Fact]
    public async Task Handle_BotMessage_ShouldSaveAndNotifyHub()
    {
        var chatRoom = ChatRoom.Create("General");
        _chatRoomRepoMock.Setup(r => r.GetByIdAsync(chatRoom.Id, default)).ReturnsAsync(chatRoom);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

        var command = new PostBotMessageCommand(chatRoom.Id, "AAPL.US quote is $150.00 per share.");
        var handler = new PostBotMessageCommandHandler(
            _chatRoomRepoMock.Object,
            _messageRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _hubNotifierMock.Object,
            Mock.Of<ILogger<PostBotMessageCommandHandler>>());

        var result = await handler.Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        _hubNotifierMock.Verify(h => h.NotifyMessageAsync(
            chatRoom.Id,
            It.Is<MessageNotification>(n => n.IsBot == true),
            default), Times.Once);
    }
}
