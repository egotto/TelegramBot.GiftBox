using System.Text.Json;
using System.Net;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.GiftBox.Application.Contracts;
using TelegramBot.GiftBox.Application.Interfaces;
using TelegramBot.GiftBox.Configuration;

namespace TelegramBot.GiftBox.Telegram;

public sealed class GiftPresentationService(
    ITelegramBotClient botClient,
    IHostEnvironment hostEnvironment) : IGiftPresentationService
{
    public async Task SendAsync(
        long recipientTelegramId,
        string languageCode,
        GiftPresentation presentation,
        CancellationToken cancellationToken)
    {
        var message = new List<string>
        {
            BotMessageCatalog.GiftActivatedTitle(languageCode, presentation.Title)
        };

        if (!string.IsNullOrWhiteSpace(presentation.Description))
        {
            message.Add(WebUtility.HtmlEncode(presentation.Description));
        }

        switch (presentation.Type)
        {
            case GiftTypes.SteamCode:
                message.Add($"{BotMessageCatalog.GiftCodeLabel(languageCode)}: {Encode(GetRequiredString(presentation.Payload, "code"))}");
                AddOptionalUrl(message, presentation.Payload, "instructionsUrl");
                break;
            case GiftTypes.SteamLink:
                AddRequiredUrl(message, presentation.Payload, "url");
                break;
            case GiftTypes.WalletCode:
                message.Add($"{BotMessageCatalog.GiftCodeLabel(languageCode)}: {Encode(GetRequiredString(presentation.Payload, "code"))}");
                message.Add($"{BotMessageCatalog.RedeemWallet(languageCode)}: {Encode(GetRequiredString(presentation.Payload, "redeemUrl"))}");
                break;
            case GiftTypes.Surprise:
                break;
            default:
                throw new InvalidOperationException("Gift type was not validated.");
        }

        if (!string.IsNullOrWhiteSpace(presentation.Image))
        {
            var imagePath = ResolveImagePath(presentation.Image);
            await using var imageStream = File.OpenRead(imagePath);
            await botClient.SendPhoto(
                recipientTelegramId,
                InputFile.FromStream(imageStream, Path.GetFileName(imagePath)),
                caption: $"🎁 <b>{Encode(presentation.Title)}</b>",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }

        await botClient.SendMessage(
            recipientTelegramId,
            string.Join("\n\n", message),
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private string ResolveImagePath(string configuredPath)
    {
        var rootPath = Path.GetFullPath(hostEnvironment.ContentRootPath);
        var imagePath = Path.GetFullPath(configuredPath, rootPath);
        var rootPrefix = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;
        if (!imagePath.StartsWith(rootPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Configured gift image path is outside the content root.");
        }

        return imagePath;
    }

    private static string GetRequiredString(JsonElement payload, string propertyName)
    {
        return payload.GetProperty(propertyName).GetString()
            ?? throw new InvalidOperationException("Validated gift payload is missing a required value.");
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static void AddRequiredUrl(List<string> message, JsonElement payload, string propertyName)
    {
        message.Add(Encode(GetRequiredString(payload, propertyName)));
    }

    private static void AddOptionalUrl(List<string> message, JsonElement payload, string propertyName)
    {
        if (payload.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(property.GetString()))
        {
            message.Add(WebUtility.HtmlEncode(property.GetString()!));
        }
    }
}
