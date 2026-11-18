using Microsoft.EntityFrameworkCore;
using TelegramBot.GiftBox.Application.Contracts;
using TelegramBot.GiftBox.Application.Interfaces;
using TelegramBot.GiftBox.Configuration;
using TelegramBot.GiftBox.Data;

namespace TelegramBot.GiftBox.Application;

public sealed class GiftCalendarService(
    GiftBoxDbContext dbContext,
    IAccessControlService accessControl,
    IGiftCalendarProvider calendarProvider,
    TimeProvider timeProvider) : IGiftCalendarService
{
    public async Task<IReadOnlyList<GiftCalendarItem>> GetCalendarAsync(
        long recipientTelegramId,
        CancellationToken cancellationToken)
    {
        var access = await accessControl.CheckAsync(recipientTelegramId, cancellationToken);
        if (!access.Role.HasFlag(AccessRole.Recipient))
        {
            throw new UnauthorizedAccessException("Recipient access is required.");
        }

        var calendar = calendarProvider.Current;
        var now = timeProvider.GetUtcNow();
        var gifts = await dbContext.Gifts
            .AsNoTracking()
            .Where(gift => gift.IsActive)
            .OrderBy(gift => gift.DayNumber)
            .Select(gift => new
            {
                Gift = gift,
                IsActivated = dbContext.Redemptions.Any(redemption =>
                    redemption.GiftId == gift.Id
                    && redemption.TelegramId == recipientTelegramId),
                IsUnlockedEarly = gift.IsUnlockedEarly
            })
            .ToListAsync(cancellationToken);

        return gifts.Select(item =>
        {
            var availableFrom = GiftAvailability.GetAvailableFrom(item.Gift, calendar);
            var isAvailable = item.IsUnlockedEarly || now >= availableFrom;
            return new GiftCalendarItem(
                item.Gift.Id,
                item.Gift.DayNumber,
                item.IsActivated ? item.Gift.Title : null,
                availableFrom,
                isAvailable,
                item.IsActivated,
                item.IsUnlockedEarly);
        }).ToArray();
    }
}
