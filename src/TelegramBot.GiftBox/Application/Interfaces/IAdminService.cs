using TelegramBot.GiftBox.Application.Contracts;

namespace TelegramBot.GiftBox.Application.Interfaces;

public interface IAdminService
{
    Task<AdminStatus> GetStatusAsync(
        long adminTelegramId,
        CancellationToken cancellationToken);

    Task UnlockAsync(
        int day,
        long adminTelegramId,
        CancellationToken cancellationToken);
}
