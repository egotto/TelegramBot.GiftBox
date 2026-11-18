using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.GiftBox.Application.Contracts;
using TelegramBot.GiftBox.Application.Interfaces;
using TelegramBot.GiftBox.Configuration;

namespace TelegramBot.GiftBox.Telegram;

public sealed class TelegramUpdateHandler(
    ITelegramBotClient botClient,
    IServiceScopeFactory scopeFactory,
    ReloadConversationStore reloadConversations,
    ILogger<TelegramUpdateHandler> logger) : ITelegramUpdateHandler, IUpdateHandler
{
    public Task HandleUpdateAsync(
        ITelegramBotClient client,
        Update update,
        CancellationToken cancellationToken)
    {
        return HandleAsync(update, cancellationToken);
    }

    public Task HandleErrorAsync(
        ITelegramBotClient client,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "Telegram polling error from {Source} ({ErrorType}).",
            source,
            exception.GetType().Name);
        return Task.CompletedTask;
    }

    public async Task HandleAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery is { } callbackQuery)
        {
            await HandleReloadCallbackAsync(callbackQuery, cancellationToken);
            return;
        }

        if (update.Message is not { From: { } sender } message)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var access = await services.GetRequiredService<IAccessControlService>()
            .CheckAsync(sender.Id, cancellationToken);
        if (access.Role == AccessRole.Denied)
        {
            await SendAsync(message.Chat.Id, BotMessageCatalog.AccessDenied(access.LanguageCode), cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(message.Text))
        {
            await SendAsync(message.Chat.Id, BotMessageCatalog.Welcome(access.LanguageCode), cancellationToken);
            return;
        }

        try
        {
            var text = message.Text;
            var (command, argument) = ParseCommand(text);
            await HandleMessageAsync(
                message.Chat.Id,
                sender.Id,
                command,
                argument,
                text,
                access.Role,
                access.LanguageCode,
                services,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            await SendAsync(message.Chat.Id, BotMessageCatalog.AccessDenied(access.LanguageCode), cancellationToken);
        }
        catch (GiftConfigurationException)
        {
            await SendAsync(message.Chat.Id, BotMessageCatalog.ReloadFailed(access.LanguageCode), cancellationToken);
        }
        catch (GiftConfigurationConflictException)
        {
            await SendAsync(message.Chat.Id, BotMessageCatalog.ReloadFailed(access.LanguageCode), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Telegram update handling failed ({ErrorType}).",
                exception.GetType().Name);
            await SendAsync(message.Chat.Id, BotMessageCatalog.UnexpectedError(access.LanguageCode), cancellationToken);
        }
    }

    private async Task HandleMessageAsync(
        long chatId,
        long telegramUserId,
        string? command,
        string? argument,
        string text,
        AccessRole roles,
        string languageCode,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        if (command == "/cancel" && roles.HasFlag(AccessRole.Administrator))
        {
            await CancelReloadAsync(chatId, telegramUserId, languageCode, cancellationToken);
            return;
        }

        if (roles.HasFlag(AccessRole.Administrator)
            && reloadConversations.HasSession(telegramUserId)
            && command is null)
        {
            await SendAsync(chatId, BotMessageCatalog.ReloadInvalidChoice(languageCode), cancellationToken);
            return;
        }

        if (command == "/start")
        {
            if (roles.HasFlag(AccessRole.Recipient) && !string.IsNullOrWhiteSpace(argument))
            {
                await RedeemAsync(chatId, telegramUserId, argument, languageCode, services, cancellationToken);
            }
            else
            {
                await SendAsync(chatId, BotMessageCatalog.Welcome(languageCode), cancellationToken);
            }

            return;
        }

        if (command == "/language")
        {
            await ChangeLanguageAsync(
                chatId,
                telegramUserId,
                argument,
                languageCode,
                services,
                cancellationToken);
            return;
        }

        if (command == "/code")
        {
            if (!roles.HasFlag(AccessRole.Recipient))
            {
                await SendAsync(chatId, BotMessageCatalog.AccessDenied(languageCode), cancellationToken);
            }
            else if (string.IsNullOrWhiteSpace(argument))
            {
                await SendAsync(chatId, BotMessageCatalog.CodePrompt(languageCode), cancellationToken);
            }
            else
            {
                await RedeemAsync(chatId, telegramUserId, argument, languageCode, services, cancellationToken);
            }

            return;
        }

        if (command == "/calendar")
        {
            if (!roles.HasFlag(AccessRole.Recipient))
            {
                await SendAsync(chatId, BotMessageCatalog.AccessDenied(languageCode), cancellationToken);
                return;
            }

            var calendarService = services.GetRequiredService<IGiftCalendarService>();
            var days = await calendarService.GetCalendarAsync(telegramUserId, cancellationToken);
            var calendarMessage = days.Count == 0
                ? BotMessageCatalog.CalendarEmpty(languageCode)
                : string.Join("\n", days.Select(day => BotMessageCatalog.CalendarDay(
                    languageCode,
                    day.Day,
                    day.IsActivated,
                    day.IsAvailable,
                    day.IsUnlockedEarly,
                    day.Title)));
            await SendAsync(chatId, calendarMessage, cancellationToken);
            return;
        }

        if (command == "/status")
        {
            if (!roles.HasFlag(AccessRole.Administrator))
            {
                await SendAsync(chatId, BotMessageCatalog.AccessDenied(languageCode), cancellationToken);
                return;
            }

            var adminService = services.GetRequiredService<IAdminService>();
            var status = await adminService.GetStatusAsync(telegramUserId, cancellationToken);
            await SendAsync(
                chatId,
                BotMessageCatalog.AdminStatus(languageCode, status.TotalGifts, status.ActivatedGifts),
                cancellationToken);
            return;
        }

        if (command == "/unlock")
        {
            if (!roles.HasFlag(AccessRole.Administrator))
            {
                await SendAsync(chatId, BotMessageCatalog.AccessDenied(languageCode), cancellationToken);
                return;
            }

            await UnlockAsync(chatId, telegramUserId, argument, languageCode, services, cancellationToken);
            return;
        }

        if (command == "/reload")
        {
            if (!roles.HasFlag(AccessRole.Administrator))
            {
                await SendAsync(chatId, BotMessageCatalog.AccessDenied(languageCode), cancellationToken);
                return;
            }

            await StartReloadAsync(chatId, telegramUserId, languageCode, services, cancellationToken);
            return;
        }

        if (command is null && roles.HasFlag(AccessRole.Recipient))
        {
            await RedeemAsync(chatId, telegramUserId, text.Trim(), languageCode, services, cancellationToken);
            return;
        }

        await SendAsync(
            chatId,
            command is null
                ? BotMessageCatalog.AccessDenied(languageCode)
                : BotMessageCatalog.Welcome(languageCode),
            cancellationToken);
    }

    private async Task RedeemAsync(
        long chatId,
        long recipientTelegramId,
        string activationCode,
        string languageCode,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var result = await services.GetRequiredService<IRedemptionService>().RedeemAsync(
            recipientTelegramId,
            activationCode,
            cancellationToken);

        switch (result.Status)
        {
            case RedemptionStatus.Activated:
            case RedemptionStatus.AlreadyActivated:
                if (result.Presentation is not null)
                {
                    await services.GetRequiredService<IGiftPresentationService>().SendAsync(
                        recipientTelegramId,
                        languageCode,
                        result.Presentation,
                        cancellationToken);
                    return;
                }

                await SendAsync(chatId, BotMessageCatalog.InvalidCode(languageCode), cancellationToken);
                break;
            case RedemptionStatus.GiftUnavailable:
                await SendAsync(chatId, BotMessageCatalog.GiftUnavailable(languageCode), cancellationToken);
                break;
            case RedemptionStatus.AlreadyClaimed:
                await SendAsync(chatId, BotMessageCatalog.AlreadyClaimed(languageCode), cancellationToken);
                break;
            default:
                await SendAsync(chatId, BotMessageCatalog.InvalidCode(languageCode), cancellationToken);
                break;
        }
    }

    private async Task ChangeLanguageAsync(
        long chatId,
        long telegramUserId,
        string? argument,
        string currentLanguageCode,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            await SendAsync(chatId, BotMessageCatalog.LanguagePrompt(currentLanguageCode), cancellationToken);
            return;
        }

        var languageCode = argument.Trim().ToLowerInvariant();
        if (!LanguageCodes.IsSupported(languageCode))
        {
            await SendAsync(chatId, BotMessageCatalog.LanguageInvalid(currentLanguageCode), cancellationToken);
            return;
        }

        await services.GetRequiredService<IUserPreferenceService>()
            .SetLanguageAsync(telegramUserId, languageCode, cancellationToken);
        await SendAsync(chatId, BotMessageCatalog.LanguageUpdated(languageCode), cancellationToken);
    }

    private async Task UnlockAsync(
        long chatId,
        long adminTelegramId,
        string? argument,
        string languageCode,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(argument, out var day) || day < 1)
        {
            await SendAsync(chatId, BotMessageCatalog.UnlockUsage(languageCode), cancellationToken);
            return;
        }

        try
        {
            await services.GetRequiredService<IAdminService>()
                .UnlockAsync(day, adminTelegramId, cancellationToken);
            await SendAsync(chatId, BotMessageCatalog.UnlockSucceeded(languageCode, day), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            await SendAsync(chatId, BotMessageCatalog.UnlockNotFound(languageCode), cancellationToken);
        }
    }

    private async Task StartReloadAsync(
        long chatId,
        long adminTelegramId,
        string languageCode,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var reloadService = services.GetRequiredService<IGiftConfigurationReloadService>();
        var preview = await reloadService.PrepareAsync(adminTelegramId, cancellationToken);
        if (preview.CodeConflicts.Count == 0)
        {
            var result = await reloadService.ApplyAsync(adminTelegramId, [], cancellationToken);
            await SendAsync(
                chatId,
                BotMessageCatalog.ReloadSucceeded(languageCode, result.GiftsUpdated),
                cancellationToken);
            return;
        }

        var choiceToken = reloadConversations.Start(adminTelegramId, preview.CodeConflicts);
        await SendReloadConflictPromptAsync(
            chatId,
            languageCode,
            preview.CodeConflicts[0],
            choiceToken,
            cancellationToken);
    }

    private async Task HandleReloadCallbackAsync(
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message?.Chat.Id ?? callbackQuery.From.Id;
        var languageCode = LanguageCodes.English;
        var callbackAnswered = false;
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var access = await services.GetRequiredService<IAccessControlService>()
                .CheckAsync(callbackQuery.From.Id, cancellationToken);
            languageCode = access.LanguageCode;
            if (!access.Role.HasFlag(AccessRole.Administrator))
            {
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    text: BotMessageCatalog.AccessDenied(languageCode),
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return;
            }

            if (!TryParseReloadChoice(callbackQuery.Data, out var choiceToken, out var resolution))
            {
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    text: BotMessageCatalog.ReloadInvalidChoice(languageCode),
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return;
            }

            var step = reloadConversations.RecordChoice(callbackQuery.From.Id, choiceToken, resolution);
            if (!step.Found)
            {
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    text: BotMessageCatalog.ReloadInvalidChoice(languageCode),
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return;
            }

            await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            callbackAnswered = true;
            if (!step.IsComplete)
            {
                await SendReloadConflictPromptAsync(
                    chatId,
                    languageCode,
                    step.Conflict!,
                    step.ChoiceToken!,
                    cancellationToken);
                return;
            }

            var result = await services.GetRequiredService<IGiftConfigurationReloadService>()
                .ApplyAsync(callbackQuery.From.Id, step.Choices, cancellationToken);
            await SendAsync(
                chatId,
                BotMessageCatalog.ReloadSucceeded(languageCode, result.GiftsUpdated),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "Telegram reload callback handling failed ({ErrorType}).",
                exception.GetType().Name);
            if (callbackAnswered)
            {
                await SendAsync(chatId, BotMessageCatalog.ReloadFailed(languageCode), cancellationToken);
            }
            else
            {
                await botClient.AnswerCallbackQuery(
                    callbackQuery.Id,
                    text: BotMessageCatalog.UnexpectedError(languageCode),
                    showAlert: true,
                    cancellationToken: cancellationToken);
            }
        }
    }

    private async Task SendReloadConflictPromptAsync(
        long chatId,
        string languageCode,
        ReloadCodeConflict conflict,
        string choiceToken,
        CancellationToken cancellationToken)
    {
        var keyboard = new InlineKeyboardMarkup(
        [
            [
                InlineKeyboardButton.WithCallbackData(
                    BotMessageCatalog.ReloadAddButton(languageCode),
                    $"reload:add:{choiceToken}"),
                InlineKeyboardButton.WithCallbackData(
                    BotMessageCatalog.ReloadReplaceButton(languageCode),
                    $"reload:replace:{choiceToken}")
            ]
        ]);
        await SendAsync(
            chatId,
            BotMessageCatalog.ReloadConflictPrompt(languageCode, conflict.Day, conflict.Title),
            cancellationToken,
            keyboard);
    }

    private static bool TryParseReloadChoice(
        string? callbackData,
        out string choiceToken,
        out ActivatedGiftCodeResolution resolution)
    {
        var parts = callbackData?.Split(':', 3);
        if (parts is { Length: 3 }
            && parts[0] == "reload"
            && Guid.TryParseExact(parts[2], "N", out _))
        {
            if (parts[1] == "add")
            {
                choiceToken = parts[2];
                resolution = ActivatedGiftCodeResolution.AddIncomingCodeAsAdditional;
                return true;
            }

            if (parts[1] == "replace")
            {
                choiceToken = parts[2];
                resolution = ActivatedGiftCodeResolution.ReplaceExistingCode;
                return true;
            }
        }

        choiceToken = string.Empty;
        resolution = default;
        return false;
    }

    private async Task CancelReloadAsync(
        long chatId,
        long adminTelegramId,
        string languageCode,
        CancellationToken cancellationToken)
    {
        var cancelled = reloadConversations.Cancel(adminTelegramId);
        await SendAsync(
            chatId,
            cancelled
                ? BotMessageCatalog.ReloadCancelled(languageCode)
                : BotMessageCatalog.ReloadInvalidChoice(languageCode),
            cancellationToken);
    }

    private Task SendAsync(
        long chatId,
        string text,
        CancellationToken cancellationToken,
        InlineKeyboardMarkup? replyMarkup = null)
    {
        return botClient.SendMessage(
            chatId,
            text,
            parseMode: ParseMode.Html,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }

    private static (string? Command, string? Argument) ParseCommand(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith('/'))
        {
            return (null, null);
        }

        var separator = trimmed.IndexOfAny([' ', '\t', '\r', '\n']);
        var commandToken = separator < 0 ? trimmed : trimmed[..separator];
        var command = commandToken.Split('@', 2)[0].ToLowerInvariant();
        var argument = separator < 0 ? null : trimmed[(separator + 1)..].Trim();
        return (command, argument);
    }
}
