using ChatApp.Application;
using ChatApp.Application.Interfaces;
using ChatApp.Bot.Services;
using ChatApp.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Override Hub notifier with bot-specific no-op (bot saves to DB; API handles real-time push)
builder.Services.AddScoped<IChatHubNotifier, BotChatHubNotifier>();

builder.Services.AddHttpClient<IStockService, StockService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "ChatAppStockBot/1.0");
});

builder.Services.AddHostedService<StockBotConsumer>();
builder.Services.AddHttpClient();

var host = builder.Build();
host.Run();
