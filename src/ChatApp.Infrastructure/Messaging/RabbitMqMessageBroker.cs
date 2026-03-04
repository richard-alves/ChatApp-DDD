using ChatApp.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace ChatApp.Infrastructure.Messaging;

public class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
}

public class RabbitMqMessageBroker(
    IOptions<RabbitMqSettings> settings,
    ILogger<RabbitMqMessageBroker> logger) : IMessageBroker, IDisposable
{
    private readonly RabbitMqSettings _settings = settings.Value;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default) where T : class
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            EnsureConnected();
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

            _channel!.ExchangeDeclare(exchange, ExchangeType.Direct, durable: true);
            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: null,
                body: body);

            logger.LogInformation("Published message to exchange {Exchange} with key {RoutingKey}", exchange, routingKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish message to RabbitMQ.");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    private void EnsureConnected()
    {
        if (_connection?.IsOpen == true && _channel?.IsOpen == true)
            return;

        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
