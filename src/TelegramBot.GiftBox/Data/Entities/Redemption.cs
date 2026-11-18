namespace TelegramBot.GiftBox.Data.Entities;

public sealed class Redemption
{
    public long Id { get; set; }

    public required string GiftId { get; set; }

    public long TelegramId { get; set; }

    public required string ActivationCode { get; set; }

    public DateTimeOffset RedeemedAt { get; set; }
}
