using Azure.Core;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Events;
using ChatApp.Domain.Exceptions;
using ChatApp.Infrastructure.Persistence;
using ChatApp.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Threading;
using Xunit;

namespace ChatApp.Tests.Domain;

public class ChatRoomTests
{
    [Fact]
    public void Create_WithValidName_ShouldCreateChatRoom()
    {
        // Arrange & Act
        var room = ChatRoom.Create("General", "General discussion");

        // Assert
        room.Should().NotBeNull();
        room.Name.Should().Be("General");
        room.Description.Should().Be("General discussion");
        room.IsActive.Should().BeTrue();
        room.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldThrowDomainException(string? name)
    {
        // Act
        var act = () => ChatRoom.Create(name!);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    [Fact]
    public void Create_ShouldRaiseChatRoomCreatedEvent()
    {
        // Arrange & Act
        var room = ChatRoom.Create("General");

        // Assert
        room.DomainEvents.Should().ContainSingle(e => e is ChatRoomCreatedEvent);
        var evt = room.DomainEvents.OfType<ChatRoomCreatedEvent>().First();
        evt.ChatRoomId.Should().Be(room.Id);
        evt.Name.Should().Be("General");
    }

    [Fact]
    public void Create_ShouldTrimName()
    {
        var room = ChatRoom.Create("  General  ");
        room.Name.Should().Be("General");
    }

    [Fact]
    public void Create_WithDescription_ShouldSetDescription()
    {
        var room = ChatRoom.Create("General", "General discussion");
        room.Description.Should().Be("General discussion");
    }

    [Fact]
    public void Create_ShouldBeActiveByDefault()
    {
        var room = ChatRoom.Create("General");
        room.IsActive.Should().BeTrue();
    }
}

public class MessageTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateMessage()
    {
        // Arrange
        var chatRoomId = Guid.NewGuid();

        // Act
        var message = Message.Create("Hello", "user-1", "Alice", chatRoomId);

        // Assert
        message.Content.Should().Be("Hello");
        message.UserId.Should().Be("user-1");
        message.Username.Should().Be("Alice");
        message.ChatRoomId.Should().Be(chatRoomId);
        message.IsBot.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyContent_ShouldThrowDomainException()
    {
        // Act
        var act = () => Message.Create("", "user-1", "Alice", Guid.NewGuid());

        // Assert
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CreateBotMessage_ShouldHaveNullUserId()
    {
        // Act
        var message = Message.CreateBotMessage("Bot response", "StockBot", Guid.NewGuid());

        // Assert
        message.UserId.Should().BeNull();
        message.IsBot.Should().BeTrue();
    }

    [Fact]
    public async Task GetLastMessagesAsync_ShouldReturnLast50_OrderedByTimestamp()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var chatRoomId = Guid.NewGuid();

        using var context = new AppDbContext(options, Mock.Of<IMediator>());

        // Criando 60 mensagens
        for (int i = 0; i < 60; i++)
        {
            await context.Messages.AddAsync(
                Message.Create($"Message {i}", "user-1", "Alice", chatRoomId));
        }
        await context.SaveChangesAsync();

        var repository = new MessageRepository(context);

        // Act
        var messages = await repository.GetLastMessagesAsync(chatRoomId, 50);

        // Assert
        messages.Should().HaveCount(50);
        messages.Should().BeInAscendingOrder(m => m.CreatedAt);
    }

    [Fact]
    public async Task GetLastMessagesAsync_ShouldReturnMessages()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var chatRoomId = Guid.NewGuid();
        using var context = new AppDbContext(options, Mock.Of<IMediator>());

        await context.Messages.AddAsync(Message.Create("Hello World", "user-1", "Alice", chatRoomId));
        await context.SaveChangesAsync();

        var repository = new MessageRepository(context);

        // Act
        var messages = await repository.GetLastMessagesAsync(chatRoomId);

        // Assert
        messages.Should().ContainSingle();
        messages.First().Content.Should().Be("Hello World");
    }

    [Fact]
    public void CreateBotMessage_ShouldCreateBotMessage()
    {
        // Act
        var message = Message.CreateBotMessage("AAPL.US quote is $150.00 per share.", "StockBot", Guid.NewGuid());

        // Assert
        message.IsBot.Should().BeTrue();
        message.Username.Should().Be("StockBot");
        message.UserId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldTrimContent()
    {
        var message = Message.Create("  Hello  ", "user-1", "Alice", Guid.NewGuid());
        message.Content.Should().Be("Hello");
    }

    [Fact]
    public void CreateBotMessage_WithEmptyContent_ShouldThrowDomainException()
    {
        var act = () => Message.CreateBotMessage("", "StockBot", Guid.NewGuid());
        act.Should().Throw<DomainException>();
    }
}
