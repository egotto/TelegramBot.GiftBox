using Telegram.Bot.Types;

namespace TelegramBot.GiftBox.Telegram;

public interface ITelegramUpdateHandler
{
    Task HandleAsync(Update update, CancellationToken cancellationToken);
}
