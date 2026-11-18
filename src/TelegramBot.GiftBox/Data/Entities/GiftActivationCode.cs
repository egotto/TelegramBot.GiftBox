namespace TelegramBot.GiftBox.Data.Entities;

public sealed class GiftActivationCode
{
    public required string ActivationCode { get; set; }

    public required string GiftId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public Gift Gift { get; set; } = null!;
}
