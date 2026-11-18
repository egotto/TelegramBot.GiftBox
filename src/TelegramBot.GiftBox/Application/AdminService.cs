using Microsoft.EntityFrameworkCore;
using TelegramBot.GiftBox.Application.Contracts;
using TelegramBot.GiftBox.Application.Interfaces;
using TelegramBot.GiftBox.Data;

namespace TelegramBot.GiftBox.Application;

public sealed class AdminService(
    GiftBoxDbContext dbContext,
    IAccessControlService accessControl) : IAdminService
{
    public async Task<AdminStatus> GetStatusAsync(
        long adminTelegramId,
        CancellationToken cancellationToken)
    {
        await EnsureAdministratorAsync(adminTelegramId, cancellationToken);

        var totalGifts = await dbContext.Gifts.CountAsync(gift => gift.IsActive, cancellationToken);
        var activatedDays = await dbContext.Redemptions
            .Join(
                dbContext.Gifts,
                redemption => redemption.GiftId,
                gift => gift.Id,
                (_, gift) => new { gift.DayNumber, gift.IsActive })
            .Where(item => item.IsActive)
            .OrderBy(item => item.DayNumber)
            .Select(item => item.DayNumber)
            .ToListAsync(cancellationToken);

        return new AdminStatus(totalGifts, activatedDays.Count, activatedDays);
    }

    public async Task UnlockAsync(
        int day,
        long adminTelegramId,
        CancellationToken cancellationToken)
    {
        await EnsureAdministratorAsync(adminTelegramId, cancellationToken);

        var gift = await dbContext.Gifts
            .SingleOrDefaultAsync(item => item.DayNumber == day && item.IsActive, cancellationToken);
        if (gift is null)
        {
            throw new KeyNotFoundException("No active gift exists for that day.");
        }

        gift.IsUnlockedEarly = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAdministratorAsync(long adminTelegramId, CancellationToken cancellationToken)
    {
        var access = await accessControl.CheckAsync(adminTelegramId, cancellationToken);
        if (!access.Role.HasFlag(AccessRole.Administrator))
        {
            throw new UnauthorizedAccessException("Administrator access is required.");
        }
    }
}
