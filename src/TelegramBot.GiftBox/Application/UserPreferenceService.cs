using Microsoft.EntityFrameworkCore;
using TelegramBot.GiftBox.Application.Contracts;
using TelegramBot.GiftBox.Application.Interfaces;
using TelegramBot.GiftBox.Configuration;
using TelegramBot.GiftBox.Data;
using TelegramBot.GiftBox.Data.Entities;

namespace TelegramBot.GiftBox.Application;

public sealed class UserPreferenceService(
    GiftBoxDbContext dbContext,
    IAccessControlService accessControl,
    TimeProvider timeProvider) : IUserPreferenceService
{
    public async Task SetLanguageAsync(
        long telegramUserId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        if (!LanguageCodes.IsSupported(languageCode))
        {
            throw new ArgumentException("Language must be 'ru', 'en', or 'pl'.", nameof(languageCode));
        }

        var access = await accessControl.CheckAsync(telegramUserId, cancellationToken);
        if (access.Role == AccessRole.Denied)
        {
            throw new UnauthorizedAccessException("Authorized access is required.");
        }

        var user = await dbContext.BotUsers
            .SingleOrDefaultAsync(candidate => candidate.TelegramId == telegramUserId, cancellationToken);
        if (user is null)
        {
            dbContext.BotUsers.Add(new BotUser
            {
                TelegramId = telegramUserId,
                LanguageCode = languageCode,
                CreatedAt = timeProvider.GetUtcNow()
            });
        }
        else
        {
            user.LanguageCode = languageCode;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
