using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TelegramBot.GiftBox.Application.Interfaces;
using TelegramBot.GiftBox.Configuration;
using TelegramBot.GiftBox.Data.Entities;

namespace TelegramBot.GiftBox.Data;

public sealed class DatabaseInitializationService(
    IServiceScopeFactory scopeFactory,
    GiftCalendarOptions calendar,
    ILogger<DatabaseInitializationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GiftBoxDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await SeedAllowedUsersAsync(dbContext, cancellationToken);

        var reloadService = scope.ServiceProvider
            .GetRequiredService<IGiftConfigurationReloadService>();
        var adminId = calendar.AdminTelegramIds[0];
        var preview = await reloadService.PrepareAsync(adminId, cancellationToken);
        if (preview.CodeConflicts.Count > 0)
        {
            logger.LogWarning(
                "Startup gift synchronization is waiting for {ConflictCount} activated-code choices.",
                preview.CodeConflicts.Count);
            return;
        }

        var result = await reloadService.ApplyAsync(adminId, [], cancellationToken);
        logger.LogInformation(
            "Gift configuration synchronized. Active gifts: {GiftCount}.",
            result.GiftsUpdated);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedAllowedUsersAsync(
        GiftBoxDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var configuredIds = calendar.RecipientTelegramIds
            .Concat(calendar.AdminTelegramIds)
            .ToHashSet();
        var existingIds = await dbContext.BotUsers
            .Where(user => configuredIds.Contains(user.TelegramId))
            .Select(user => user.TelegramId)
            .ToListAsync(cancellationToken);
        var existingSet = existingIds.ToHashSet();
        var now = DateTimeOffset.UtcNow;

        foreach (var telegramId in configuredIds.Except(existingSet))
        {
            dbContext.BotUsers.Add(new BotUser
            {
                TelegramId = telegramId,
                LanguageCode = calendar.DefaultLanguage,
                CreatedAt = now
            });
        }

        foreach (var telegramId in existingSet)
        {
            var user = await dbContext.BotUsers
                .SingleAsync(item => item.TelegramId == telegramId, cancellationToken);
            user.IsActive = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
