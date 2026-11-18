namespace TelegramBot.GiftBox.Configuration;

public sealed class GiftConfigurationFileOptions
{
    public const string SectionName = "GiftConfiguration";

    public string Path { get; set; } = "config/gifts.json";
}
