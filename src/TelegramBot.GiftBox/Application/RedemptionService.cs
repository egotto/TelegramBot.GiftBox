using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TelegramBot.GiftBox.Application.Contracts;
using TelegramBot.GiftBox.Application.Interfaces;
using TelegramBot.GiftBox.Configuration;
using TelegramBot.GiftBox.Data;
using TelegramBot.GiftBox.Data.Entities;

namespace TelegramBot.GiftBox.Application;

public sealed class RedemptionService(
    GiftBoxDbContext dbContext,
    IAccessControlService accessControl,
    IGiftCalendarProvider calendarProvider,
    TimeProvider timeProvider) : IRedemptionService
{
    public async Task<RedemptionResult> RedeemAsync(
        long recipientTelegramId,
        string activationCode,
        CancellationToken cancellationToken)
    {
        var access = await accessControl.CheckAsync(recipientTelegramId, cancellationToken);
        if (!access.Role.HasFlag(AccessRole.Recipient))
        {
            return new RedemptionResult(RedemptionStatus.Unauthorized, null);
        }

        string normalizedCode;
        try
        {
            normalizedCode = ActivationCode.Normalize(activationCode);
        }
        catch (ArgumentException)
        {
            return new RedemptionResult(RedemptionStatus.InvalidActivationCode, null);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        string? candidateGiftId = null;

        try
        {
            var activationCodeRecord = await dbContext.GiftActivationCodes
                .AsNoTracking()
                .Where(code => code.ActivationCode == normalizedCode && code.IsActive)
                .Select(code => new
                {
                    code.GiftId,
                    Gift = code.Gift
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (activationCodeRecord?.Gift is not { IsActive: true } gift)
            {
                await transaction.CommitAsync(cancellationToken);
                return new RedemptionResult(RedemptionStatus.InvalidActivationCode, null);
            }

            candidateGiftId = gift.Id;
            var existingRedemption = await dbContext.Redemptions
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.GiftId == gift.Id, cancellationToken);

            if (existingRedemption is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return existingRedemption.TelegramId == recipientTelegramId
                    ? new RedemptionResult(
                        RedemptionStatus.AlreadyActivated,
                        ToPresentation(gift))
                    : new RedemptionResult(RedemptionStatus.AlreadyClaimed, null);
            }

            var calendar = calendarProvider.Current;
            var earliestAvailability = GiftAvailability.GetAvailableFrom(gift, calendar);

            if (!gift.IsUnlockedEarly && timeProvider.GetUtcNow() < earliestAvailability)
            {
                await transaction.CommitAsync(cancellationToken);
                return new RedemptionResult(RedemptionStatus.GiftUnavailable, null);
            }

            var botUser = await dbContext.BotUsers
                .SingleOrDefaultAsync(item => item.TelegramId == recipientTelegramId, cancellationToken);
            if (botUser is null)
            {
                dbContext.BotUsers.Add(new BotUser
                {
                    TelegramId = recipientTelegramId,
                    LanguageCode = access.LanguageCode,
                    CreatedAt = timeProvider.GetUtcNow()
                });
            }

            dbContext.Redemptions.Add(new Redemption
            {
                GiftId = gift.Id,
                TelegramId = recipientTelegramId,
                ActivationCode = normalizedCode,
                RedeemedAt = timeProvider.GetUtcNow()
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new RedemptionResult(RedemptionStatus.Activated, ToPresentation(gift));
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();

            var existingRedemption = await dbContext.Redemptions
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.ActivationCode == normalizedCode, cancellationToken);

            if (existingRedemption is null && candidateGiftId is not null)
            {
                existingRedemption = await dbContext.Redemptions
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.GiftId == candidateGiftId, cancellationToken);
            }

            if (existingRedemption is null)
            {
                return new RedemptionResult(RedemptionStatus.AlreadyClaimed, null);
            }

            if (existingRedemption.TelegramId != recipientTelegramId)
            {
                return new RedemptionResult(RedemptionStatus.AlreadyClaimed, null);
            }

            var existingGift = await dbContext.Gifts
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == existingRedemption.GiftId, cancellationToken);

            return existingGift is null
                ? new RedemptionResult(RedemptionStatus.AlreadyClaimed, null)
                : new RedemptionResult(
                    RedemptionStatus.AlreadyActivated,
                    ToPresentation(existingGift));
        }
    }

    private static GiftPresentation ToPresentation(Data.Entities.Gift gift)
    {
        using var payload = JsonDocument.Parse(gift.Payload);
        return new GiftPresentation(
            gift.Id,
            gift.DayNumber,
            gift.GiftType,
            gift.Title,
            gift.Description,
            gift.Image,
            payload.RootElement.Clone());
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        for (var current = exception.InnerException; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return true;
            }
        }

        return false;
    }

}
