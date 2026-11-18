using TelegramBot.GiftBox.Configuration;

namespace TelegramBot.GiftBox.Data.Entities;

public sealed class BotUser
{
    public long TelegramId { get; set; }

    public string? DisplayName { get; set; }

    public string LanguageCode { get; set; } = LanguageCodes.Default;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
}
