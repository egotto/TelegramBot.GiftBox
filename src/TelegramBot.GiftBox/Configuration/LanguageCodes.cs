namespace TelegramBot.GiftBox.Configuration;

public static class LanguageCodes
{
    public const string Russian = "ru";
    public const string English = "en";
    public const string Polish = "pl";
    public const string Default = Russian;

    public static bool IsSupported(string? languageCode)
    {
        return languageCode is Russian or English or Polish;
    }
}
