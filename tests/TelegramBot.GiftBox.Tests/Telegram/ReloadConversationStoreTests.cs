using TelegramBot.GiftBox.Application.Contracts;
using TelegramBot.GiftBox.Telegram;

namespace TelegramBot.GiftBox.Tests.Telegram;

public sealed class ReloadConversationStoreTests
{
    [Fact]
    public void RecordChoice_CollectsChoicesInConflictOrder()
    {
        var store = new ReloadConversationStore();
        var choiceToken = store.Start(987654321,
        [
            new ReloadCodeConflict("day02", 2, "First gift"),
            new ReloadCodeConflict("day07", 7, "Second gift")
        ]);

        var firstStep = store.RecordChoice(
            987654321,
            choiceToken,
            ActivatedGiftCodeResolution.AddIncomingCodeAsAdditional);
        var finalStep = store.RecordChoice(
            987654321,
            firstStep.ChoiceToken!,
            ActivatedGiftCodeResolution.ReplaceExistingCode);

        Assert.True(firstStep.Found);
        Assert.False(firstStep.IsComplete);
        Assert.Equal("day07", firstStep.Conflict!.GiftId);
        Assert.True(finalStep.Found);
        Assert.True(finalStep.IsComplete);
        Assert.Collection(
            finalStep.Choices,
            choice => Assert.Equal(ActivatedGiftCodeResolution.AddIncomingCodeAsAdditional, choice.Resolution),
            choice => Assert.Equal(ActivatedGiftCodeResolution.ReplaceExistingCode, choice.Resolution));
        Assert.False(store.HasSession(987654321));
    }

    [Fact]
    public void RecordChoice_StaleTokenDoesNotAdvanceSession()
    {
        var store = new ReloadConversationStore();
        var choiceToken = store.Start(987654321, [new ReloadCodeConflict("day02", 2, "First gift")]);

        var staleStep = store.RecordChoice(
            987654321,
            Guid.NewGuid().ToString("N"),
            ActivatedGiftCodeResolution.ReplaceExistingCode);
        Assert.False(staleStep.Found);
        Assert.True(store.HasSession(987654321));

        var validStep = store.RecordChoice(
            987654321,
            choiceToken,
            ActivatedGiftCodeResolution.AddIncomingCodeAsAdditional);

        Assert.True(validStep.Found);
        Assert.True(validStep.IsComplete);
    }

    [Fact]
    public void Cancel_RemovesPendingSession()
    {
        var store = new ReloadConversationStore();
        store.Start(987654321, [new ReloadCodeConflict("day02", 2, "First gift")]);

        Assert.True(store.Cancel(987654321));
        Assert.False(store.HasSession(987654321));
    }
}
