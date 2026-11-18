namespace TelegramBot.GiftBox.Application.Contracts;

public sealed record AdminStatus(
    int TotalGifts,
    int ActivatedGifts,
    IReadOnlyList<int> ActivatedDays);

public enum ActivatedGiftCodeResolution
{
    AddIncomingCodeAsAdditional,
    ReplaceExistingCode
}

public sealed record ReloadCodeConflict(string GiftId, int Day, string Title);

public sealed record ReloadCodeChoice(
    string GiftId,
    ActivatedGiftCodeResolution Resolution);

public sealed record ReloadPreview(
    IReadOnlyList<ReloadCodeConflict> CodeConflicts);

public sealed record ReloadResult(int GiftsUpdated);
