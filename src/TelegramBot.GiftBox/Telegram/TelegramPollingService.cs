using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace TelegramBot.GiftBox.Telegram;

public sealed class TelegramPollingService(
    ITelegramBotClient botClient,
    TelegramUpdateHandler updateHandler,
    ILogger<TelegramPollingService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
            DropPendingUpdates = false
        };

        logger.LogInformation("Starting Telegram long polling.");
        botClient.StartReceiving(
            updateHandler.HandleUpdateAsync,
            updateHandler.HandleErrorAsync,
            receiverOptions,
            stoppingToken);

        return Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
