using ChatApp.Application.Messages.Commands;
using ChatApp.Bot.Services;
using ChatApp.Infrastructure.Messaging;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Http.Json;
using System.Text;

namespace ChatApp.Bot.Services;

public class StockBotConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqSettings> rabbitSettings,
    ILogger<StockBotConsumer> logger) : BackgroundService
{
    private const string Exchange = "stock.exchange";
    private const string Queue = "stock.query.queue";
    private const string RoutingKey = "stock.query";

    private IConnection? _connection;
    private IModel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StockBot Consumer starting...");

        await ConnectWithRetryAsync(stoppingToken);

        stoppingToken.Register(() =>
        {
            logger.LogInformation("StockBot Consumer stopping.");
            _channel?.Close();
            _connection?.Close();
        });

        // Keep running
        //while (!stoppingToken.IsCancellationRequested)
        //    await Task.Delay(1000, stoppingToken).ContinueWith(_ => { });

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
    {
        var settings = rabbitSettings.Value;
        var retryCount = 0;
        const int maxRetries = 10;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = settings.Host,
                    Port = settings.Port,
                    UserName = settings.Username,
                    Password = settings.Password,
                    VirtualHost = settings.VirtualHost,
                    DispatchConsumersAsync = true
                };

                _connection = factory.CreateConnection("StockBot");
                _channel = _connection.CreateModel();

                _channel.ExchangeDeclare(Exchange, ExchangeType.Direct, durable: true);
                _channel.QueueDeclare(Queue, durable: true, exclusive: false, autoDelete: false);
                _channel.QueueBind(Queue, Exchange, RoutingKey);
                _channel.BasicQos(0, 1, false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += OnMessageReceivedAsync;
                _channel.BasicConsume(Queue, autoAck: false, consumer: consumer);

                logger.LogInformation("StockBot conectado e escutando...");
                return;
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogWarning("Tentativa {Retry}/{Max} falhou: {Error}", retryCount, maxRetries, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs e)
    {
        var body = Encoding.UTF8.GetString(e.Body.ToArray());
        logger.LogInformation("StockBot received message: {Body}", body);
        try
        {
            var queryMessage = JsonConvert.DeserializeObject<StockQueryMessage>(body);
            if (queryMessage is null)
            {
                logger.LogWarning("Could not deserialize stock query message.");
                _channel?.BasicNack(e.DeliveryTag, false, false);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();

            var quote = await stockService.GetStockQuoteAsync(queryMessage.StockCode);
            var botMessage = quote?.DisplayMessage ?? $"Could not retrieve quote for {queryMessage.StockCode.ToUpper()}.";

            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            await http.PostAsJsonAsync(
                $"https://localhost:64707/api/chatrooms/{queryMessage.ChatRoomId}/messages/bot",
                new { content = botMessage });

            _channel?.BasicAck(e.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing stock query message.");
            _channel?.BasicNack(e.DeliveryTag, false, requeue: false);
        }
    }
}
