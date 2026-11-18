using TelegramBot.GiftBox.Application.Contracts;

namespace TelegramBot.GiftBox.Application.Interfaces;

public interface IGiftCalendarService
{
    Task<IReadOnlyList<GiftCalendarItem>> GetCalendarAsync(
        long recipientTelegramId,
        CancellationToken cancellationToken);
}
