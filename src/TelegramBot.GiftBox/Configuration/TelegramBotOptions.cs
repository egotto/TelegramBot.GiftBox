namespace TelegramBot.GiftBox.Configuration;

public sealed class TelegramBotOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; init; } = string.Empty;

    public string? BaseUrl { get; init; }
}
