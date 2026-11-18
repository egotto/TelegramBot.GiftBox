using System.Text.Json;

namespace TelegramBot.GiftBox.Application.Contracts;

public sealed record GiftPresentation(
    string GiftId,
    int Day,
    string Type,
    string Title,
    string? Description,
    string? Image,
    JsonElement Payload);
