namespace TelegramBot.GiftBox.Application.Contracts;

public enum RedemptionStatus
{
    Activated,
    AlreadyActivated,
    InvalidActivationCode,
    GiftUnavailable,
    AlreadyClaimed,
    Unauthorized
}

public sealed record RedemptionResult(
    RedemptionStatus Status,
    GiftPresentation? Presentation);
