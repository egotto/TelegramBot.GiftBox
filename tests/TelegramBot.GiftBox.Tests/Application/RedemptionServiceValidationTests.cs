using Microsoft.EntityFrameworkCore;
using TelegramBot.GiftBox.Application;
using TelegramBot.GiftBox.Application.Contracts;
using TelegramBot.GiftBox.Application.Interfaces;
using TelegramBot.GiftBox.Configuration;
using TelegramBot.GiftBox.Data;

namespace TelegramBot.GiftBox.Tests.Application;

public sealed class RedemptionServiceValidationTests : IDisposable
{
    private readonly GiftBoxDbContext _dbContext;

    public RedemptionServiceValidationTests()
    {
        var options = new DbContextOptionsBuilder<GiftBoxDbContext>()
            .UseNpgsql("Host=localhost;Database=telegrambot_giftbox;Username=test;Password=test")
            .Options;
        _dbContext = new GiftBoxDbContext(options);
    }

    [Fact]
    public async Task RedeemAsync_UnauthorizedUser_ReturnsUnauthorizedWithoutDatabaseAccess()
    {
        var service = CreateService(AccessRole.Denied);

        var result = await service.RedeemAsync(222222222, "WINTER-01-X9K2", CancellationToken.None);

        Assert.Equal(RedemptionStatus.Unauthorized, result.Status);
        Assert.Null(result.Presentation);
    }

    [Fact]
    public async Task RedeemAsync_UserWithRecipientAndAdministratorRoles_IsRecognizedAsRecipient()
    {
        var service = CreateService(AccessRole.Recipient | AccessRole.Administrator);

        var result = await service.RedeemAsync(123456789, "not valid", CancellationToken.None);

        Assert.Equal(RedemptionStatus.InvalidActivationCode, result.Status);
    }

    [Fact]
    public async Task RedeemAsync_InvalidCode_ReturnsGenericFailureWithoutDatabaseAccess()
    {
        var service = CreateService(AccessRole.Recipient);

        var result = await service.RedeemAsync(123456789, "not valid", CancellationToken.None);

        Assert.Equal(RedemptionStatus.InvalidActivationCode, result.Status);
        Assert.Null(result.Presentation);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private RedemptionService CreateService(AccessRole role)
    {
        return new RedemptionService(
            _dbContext,
            new FixedAccessControlService(role),
            new FixedCalendarProvider(),
            TimeProvider.System);
    }

    private sealed class FixedAccessControlService(AccessRole role) : IAccessControlService
    {
        public Task<AccessDecision> CheckAsync(long telegramUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AccessDecision(role, "ru"));
        }
    }

    private sealed class FixedCalendarProvider : IGiftCalendarProvider
    {
        public GiftCalendarOptions Current { get; } = new()
        {
            CalendarName = "Test calendar",
            StartDate = new DateOnly(2026, 12, 1),
            EndDate = new DateOnly(2026, 12, 24),
            RecipientTelegramIds = [123456789],
            AdminTelegramIds = [987654321],
            Gifts = []
        };

        public void Replace(GiftCalendarOptions options)
        {
            throw new NotSupportedException();
        }
    }
}
