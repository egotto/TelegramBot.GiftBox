namespace TelegramBot.GiftBox.Application.Contracts;

[Flags]
public enum AccessRole
{
    Denied = 0,
    Recipient = 1,
    Administrator = 2
}

public sealed record AccessDecision(AccessRole Role, string LanguageCode);
