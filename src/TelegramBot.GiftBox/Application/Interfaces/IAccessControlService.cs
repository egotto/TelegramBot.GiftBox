using TelegramBot.GiftBox.Application.Contracts;

namespace TelegramBot.GiftBox.Application.Interfaces;

public interface IAccessControlService
{
    Task<AccessDecision> CheckAsync(
        long telegramUserId,
        CancellationToken cancellationToken);
}
