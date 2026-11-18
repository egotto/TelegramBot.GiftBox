using TelegramBot.GiftBox.Application.Contracts;

namespace TelegramBot.GiftBox.Application.Interfaces;

public interface IRedemptionService
{
    Task<RedemptionResult> RedeemAsync(
        long recipientTelegramId,
        string activationCode,
        CancellationToken cancellationToken);
}
