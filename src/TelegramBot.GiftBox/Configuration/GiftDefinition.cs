using System.Text.Json;

namespace TelegramBot.GiftBox.Configuration;

public sealed record GiftDefinition
{
    public required string Id { get; init; }

    public required int Day { get; init; }

    public DateTimeOffset? AvailableFrom { get; init; }

    public required string ActivationCode { get; init; }

    public required string Type { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public string? Image { get; init; }

    public required JsonElement Payload { get; init; }
}
