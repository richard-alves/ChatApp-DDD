using ChatApp.Bot.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace ChatApp.Tests.Infrastructure;

public class StockServiceTests
{
    private static HttpClient CreateHttpClientWithResponse(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://stooq.com")
        };
    }

    [Fact]
    public async Task GetStockQuoteAsync_WithValidCsv_ShouldReturnQuote()
    {
        // Arrange
        var csv = "Symbol,Date,Time,Open,High,Low,Close,Volume\nAAPL.US,2024-01-01,12:00:00,150.00,155.00,149.00,152.50,1000000";
        var httpClient = CreateHttpClientWithResponse(csv);
        var logger = Mock.Of<ILogger<StockService>>();
        var service = new StockService(httpClient, logger);

        // Act
        var result = await service.GetStockQuoteAsync("aapl.us");

        // Assert
        result.Should().NotBeNull();
        result!.Price.Should().Be(152.50m);
        result.DisplayMessage.Should().Contain("per share");
    }

    [Fact]
    public async Task GetStockQuoteAsync_WithNAClose_ShouldReturnUnavailableMessage()
    {
        // Arrange
        var csv = "Symbol,Date,Time,Open,High,Low,Close,Volume\nINVALID,N/D,N/D,N/D,N/D,N/D,N/D,N/D";
        var httpClient = CreateHttpClientWithResponse(csv);
        var logger = Mock.Of<ILogger<StockService>>();
        var service = new StockService(httpClient, logger);

        // Act
        var result = await service.GetStockQuoteAsync("invalid");

        // Assert
        result.Should().NotBeNull();
        result!.Price.Should().BeNull();
        result.DisplayMessage.Should().Contain("not available");
    }

    [Fact]
    public async Task GetStockQuoteAsync_WithHttpError_ShouldReturnErrorMessage()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handlerMock.Object);
        var logger = Mock.Of<ILogger<StockService>>();
        var service = new StockService(httpClient, logger);

        // Act
        var result = await service.GetStockQuoteAsync("aapl.us");

        // Assert
        result.Should().NotBeNull();
        result!.Price.Should().BeNull();
        result.DisplayMessage.Should().Contain("Could not fetch");
    }

    [Fact]
    public async Task GetStockQuoteAsync_WithEmptyResponse_ShouldReturnUnavailable()
    {
        // Arrange
        var httpClient = CreateHttpClientWithResponse("");
        var logger = Mock.Of<ILogger<StockService>>();
        var service = new StockService(httpClient, logger);

        // Act
        var result = await service.GetStockQuoteAsync("aapl.us");

        // Assert
        result.Should().NotBeNull();
        result!.Price.Should().BeNull();
    }
}
