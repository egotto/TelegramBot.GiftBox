using TelegramBot.GiftBox.Application.Contracts;

namespace TelegramBot.GiftBox.Application.Interfaces;

public interface IGiftPresentationService
{
    Task SendAsync(
        long recipientTelegramId,
        string languageCode,
        GiftPresentation presentation,
        CancellationToken cancellationToken);
}
