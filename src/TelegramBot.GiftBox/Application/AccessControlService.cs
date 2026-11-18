using Microsoft.EntityFrameworkCore;
using TelegramBot.GiftBox.Application.Contracts;
using TelegramBot.GiftBox.Application.Interfaces;
using TelegramBot.GiftBox.Configuration;
using TelegramBot.GiftBox.Data;

namespace TelegramBot.GiftBox.Application;

public sealed class AccessControlService(
    GiftBoxDbContext dbContext,
    IGiftCalendarProvider calendarProvider) : IAccessControlService
{
    public async Task<AccessDecision> CheckAsync(
        long telegramUserId,
        CancellationToken cancellationToken)
    {
        var calendar = calendarProvider.Current;
        AccessRole role = AccessRole.Denied;
        
        if (calendar.AdminTelegramIds.Contains(telegramUserId))
        {
            role |= AccessRole.Administrator;
        }
        if (calendar.RecipientTelegramIds.Contains(telegramUserId))
        {
            role |= AccessRole.Recipient;
        }
        if (role == AccessRole.Denied)
        {
            return new AccessDecision(AccessRole.Denied, calendar.DefaultLanguage);
        }

        var user = await dbContext.BotUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.TelegramId == telegramUserId, cancellationToken);

        if (user is { IsActive: false })
        {
            return new AccessDecision(AccessRole.Denied, calendar.DefaultLanguage);
        }

        var languageCode = user is { LanguageCode: var userLanguageCode }
            && LanguageCodes.IsSupported(userLanguageCode)
            ? userLanguageCode
            : calendar.DefaultLanguage;

        return new AccessDecision(role, languageCode);
    }
}
