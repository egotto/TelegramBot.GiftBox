namespace TelegramBot.GiftBox.Configuration;

public sealed record GiftCalendarOptions
{
    public const string SectionName = "GiftCalendar";

    public required string CalendarName { get; init; }

    public required DateOnly StartDate { get; init; }

    public required DateOnly EndDate { get; init; }

    public string TimeZone { get; init; } = "Europe/Warsaw";

    public string DefaultLanguage { get; init; } = LanguageCodes.Default;

    public required List<long> RecipientTelegramIds { get; init; }

    public required List<long> AdminTelegramIds { get; init; }

    public required List<GiftDefinition> Gifts { get; init; }
}
