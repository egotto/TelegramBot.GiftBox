using TelegramBot.GiftBox.Configuration;
using TelegramBot.GiftBox.Data.Entities;

namespace TelegramBot.GiftBox.Application;

internal static class GiftAvailability
{
    public static DateTimeOffset GetAvailableFrom(Gift gift, GiftCalendarOptions calendar)
    {
        return GetAvailableFrom(gift.AvailableFrom, gift.DayNumber, calendar);
    }

    public static DateTimeOffset GetAvailableFrom(
        DateTimeOffset? configuredAvailability,
        int dayNumber,
        GiftCalendarOptions calendar)
    {
        if (configuredAvailability is { } availableFrom)
        {
            return availableFrom.ToUniversalTime();
        }

        var date = calendar.StartDate.AddDays(dayNumber - 1);
        var localMidnight = DateTime.SpecifyKind(
            date.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Unspecified);
        var utcMidnight = TimeZoneInfo.ConvertTimeToUtc(
            localMidnight,
            TimeZoneInfo.FindSystemTimeZoneById(calendar.TimeZone));

        return new DateTimeOffset(utcMidnight);
    }
}
