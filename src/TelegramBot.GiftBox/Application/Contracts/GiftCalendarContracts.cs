namespace TelegramBot.GiftBox.Application.Contracts;

public sealed record GiftCalendarItem(
    string GiftId,
    int Day,
    string? Title,
    DateTimeOffset? AvailableFrom,
    bool IsAvailable,
    bool IsActivated,
    bool IsUnlockedEarly);
