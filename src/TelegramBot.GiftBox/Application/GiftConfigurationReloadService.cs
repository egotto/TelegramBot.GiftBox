using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TelegramBot.GiftBox.Application.Contracts;
using TelegramBot.GiftBox.Application.Interfaces;
using TelegramBot.GiftBox.Configuration;
using TelegramBot.GiftBox.Data;
using TelegramBot.GiftBox.Data.Entities;

namespace TelegramBot.GiftBox.Application;

public sealed class GiftConfigurationReloadService(
    GiftBoxDbContext dbContext,
    IAccessControlService accessControl,
    GiftConfigurationLoader configurationLoader,
    IOptions<GiftConfigurationFileOptions> fileOptions,
    IHostEnvironment hostEnvironment,
    IGiftCalendarProvider calendarProvider,
    TimeProvider timeProvider) : IGiftConfigurationReloadService
{
    public async Task<ReloadPreview> PrepareAsync(
        long adminTelegramId,
        CancellationToken cancellationToken)
    {
        await EnsureAdministratorAsync(adminTelegramId, cancellationToken);
        var incoming = await LoadConfigurationAsync(cancellationToken);
        var conflicts = await FindCodeConflictsAsync(incoming, cancellationToken);
        return new ReloadPreview(conflicts);
    }

    public async Task<ReloadResult> ApplyAsync(
        long adminTelegramId,
        IReadOnlyCollection<ReloadCodeChoice> codeChoices,
        CancellationToken cancellationToken)
    {
        await EnsureAdministratorAsync(adminTelegramId, cancellationToken);
        var incoming = await LoadConfigurationAsync(cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var conflicts = await FindCodeConflictsAsync(incoming, cancellationToken);
        var conflictIds = conflicts.Select(conflict => conflict.GiftId).ToHashSet(StringComparer.Ordinal);
        var choiceMap = codeChoices.ToDictionary(choice => choice.GiftId, StringComparer.Ordinal);

        if (choiceMap.Count != codeChoices.Count
            || !conflictIds.SetEquals(choiceMap.Keys))
        {
            throw new GiftConfigurationConflictException(
                "A reload choice is required for every activated gift whose code changed.");
        }

        var gifts = await dbContext.Gifts
            .Include(gift => gift.ActivationCodes)
            .ToDictionaryAsync(gift => gift.Id, StringComparer.Ordinal, cancellationToken);
        var activatedGiftIds = (await dbContext.Redemptions
                .Select(redemption => redemption.GiftId)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
        var configuredGiftIds = incoming.Gifts
            .Select(gift => gift.Id)
            .ToHashSet(StringComparer.Ordinal);
        var now = timeProvider.GetUtcNow();

        foreach (var removedGift in gifts.Values.Where(gift => !configuredGiftIds.Contains(gift.Id)))
        {
            removedGift.IsActive = false;
            removedGift.UpdatedAt = now;
            foreach (var oldCode in removedGift.ActivationCodes)
            {
                oldCode.IsActive = false;
            }
        }

        foreach (var definition in incoming.Gifts)
        {
            if (!gifts.TryGetValue(definition.Id, out var gift))
            {
                gift = new Gift
                {
                    Id = definition.Id,
                    DayNumber = definition.Day,
                    GiftType = definition.Type,
                    Title = definition.Title,
                    Image = definition.Image,
                    Payload = definition.Payload.GetRawText(),
                    UpdatedAt = now
                };
                dbContext.Gifts.Add(gift);
                gifts.Add(gift.Id, gift);
            }

            var incomingCode = NormalizeActivationCode(definition.ActivationCode);
            var codeOwner = await dbContext.GiftActivationCodes
                .Where(code => code.ActivationCode == incomingCode)
                .Select(code => code.GiftId)
                .SingleOrDefaultAsync(cancellationToken);

            if (codeOwner is not null && codeOwner != gift.Id)
            {
                throw new GiftConfigurationException(
                    $"Activation code for gift '{gift.Id}' is already assigned to another gift.");
            }

            var activated = activatedGiftIds.Contains(gift.Id);
            var codeAlreadyActive = gift.ActivationCodes.Any(code =>
                code.ActivationCode == incomingCode && code.IsActive);
            var hasCodeConflict = activated && !codeAlreadyActive;

            if (hasCodeConflict)
            {
                var choice = choiceMap[gift.Id];
                if (choice.Resolution == ActivatedGiftCodeResolution.ReplaceExistingCode)
                {
                    foreach (var oldCode in gift.ActivationCodes)
                    {
                        oldCode.IsActive = false;
                    }
                }
            }
            else if (!activated)
            {
                foreach (var oldCode in gift.ActivationCodes)
                {
                    oldCode.IsActive = oldCode.ActivationCode == incomingCode;
                }
            }

            EnsureIncomingCode(gift, incomingCode, now);
            gift.DayNumber = definition.Day;
            gift.GiftType = definition.Type;
            gift.Title = definition.Title;
            gift.Description = definition.Description;
            gift.Image = definition.Image;
            gift.Payload = definition.Payload.GetRawText();
            gift.AvailableFrom = GiftAvailability.GetAvailableFrom(
                definition.AvailableFrom,
                definition.Day,
                incoming);
            gift.IsActive = true;
            gift.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        calendarProvider.Replace(incoming);

        return new ReloadResult(incoming.Gifts.Count);
    }

    private async Task EnsureAdministratorAsync(long adminTelegramId, CancellationToken cancellationToken)
    {
        var access = await accessControl.CheckAsync(adminTelegramId, cancellationToken);
        if (!access.Role.HasFlag(AccessRole.Administrator))
        {
            throw new UnauthorizedAccessException("Administrator access is required.");
        }
    }

    private Task<GiftCalendarOptions> LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        var path = fileOptions.Value.Path;
        return configurationLoader.LoadAsync(
            path,
            cancellationToken,
            hostEnvironment.ContentRootPath);
    }

    private async Task<IReadOnlyList<ReloadCodeConflict>> FindCodeConflictsAsync(
        GiftCalendarOptions incoming,
        CancellationToken cancellationToken)
    {
        var activatedGiftIds = (await dbContext.Redemptions
                .Select(redemption => redemption.GiftId)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
        if (activatedGiftIds.Count == 0)
        {
            return [];
        }

        var configuredGiftIds = incoming.Gifts
            .Where(gift => activatedGiftIds.Contains(gift.Id))
            .Select(gift => gift.Id)
            .ToHashSet(StringComparer.Ordinal);
        if (configuredGiftIds.Count == 0)
        {
            return [];
        }

        var currentCodes = await dbContext.GiftActivationCodes
            .AsNoTracking()
            .Where(code => code.IsActive && configuredGiftIds.Contains(code.GiftId))
            .Select(code => new { code.GiftId, code.ActivationCode })
            .ToListAsync(cancellationToken);
        var activeCodesByGift = currentCodes
            .GroupBy(code => code.GiftId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(code => code.ActivationCode).ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);

        var conflicts = new List<ReloadCodeConflict>();
        foreach (var definition in incoming.Gifts.Where(gift => activatedGiftIds.Contains(gift.Id)))
        {
            var incomingCode = NormalizeActivationCode(definition.ActivationCode);
            if (!activeCodesByGift.TryGetValue(definition.Id, out var activeCodes)
                || !activeCodes.Contains(incomingCode))
            {
                conflicts.Add(new ReloadCodeConflict(definition.Id, definition.Day, definition.Title));
            }
        }

        return conflicts;
    }

    private static string NormalizeActivationCode(string activationCode)
    {
        return ActivationCode.Normalize(activationCode);
    }

    private static void EnsureIncomingCode(Gift gift, string activationCode, DateTimeOffset now)
    {
        var existingCode = gift.ActivationCodes.SingleOrDefault(code =>
            code.ActivationCode == activationCode);
        if (existingCode is null)
        {
            var codeRecord = new GiftActivationCode
            {
                ActivationCode = activationCode,
                GiftId = gift.Id,
                IsActive = true,
                CreatedAt = now,
                Gift = gift
            };
            gift.ActivationCodes.Add(codeRecord);
        }
        else
        {
            existingCode.IsActive = true;
        }
    }

}
