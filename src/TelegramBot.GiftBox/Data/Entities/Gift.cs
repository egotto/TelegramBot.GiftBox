namespace TelegramBot.GiftBox.Data.Entities;

public sealed class Gift
{
    public required string Id { get; set; }

    public int DayNumber { get; set; }

    public required string GiftType { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public string? Image { get; set; }

    public required string Payload { get; set; }

    public DateTimeOffset? AvailableFrom { get; set; }

    public bool IsUnlockedEarly { get; set; }

    public bool IsActive { get; set; } = true;

    public string? ConfigVersion { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<GiftActivationCode> ActivationCodes { get; } = new List<GiftActivationCode>();
}
