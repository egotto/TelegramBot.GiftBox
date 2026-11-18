namespace TelegramBot.GiftBox.Application.Interfaces;

public interface IUserPreferenceService
{
    Task SetLanguageAsync(
        long telegramUserId,
        string languageCode,
        CancellationToken cancellationToken);
}
