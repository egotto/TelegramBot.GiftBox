using TelegramBot.GiftBox.Application.Contracts;

namespace TelegramBot.GiftBox.Application.Interfaces;

public interface IGiftConfigurationReloadService
{
    Task<ReloadPreview> PrepareAsync(
        long adminTelegramId,
        CancellationToken cancellationToken);

    Task<ReloadResult> ApplyAsync(
        long adminTelegramId,
        IReadOnlyCollection<ReloadCodeChoice> codeChoices,
        CancellationToken cancellationToken);
}
