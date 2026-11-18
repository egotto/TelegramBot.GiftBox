using TelegramBot.GiftBox.Telegram;

namespace TelegramBot.GiftBox.Tests.Telegram;

public sealed class BotMessageCatalogTests
{
    [Fact]
    public void Welcome_UnknownLanguage_FallsBackToEnglish()
    {
        Assert.Equal(
            "🎄 <b>Welcome!</b>\nSend the activation code from your scratch card, or use <code>/code</code> to enter it.",
            BotMessageCatalog.Welcome("de"));
    }

    [Fact]
    public void Welcome_Russian_ReturnsRussianText()
    {
        Assert.Equal(
            "🎄 <b>Добро пожаловать!</b>\nОтправьте код с карточки или используйте <code>/code</code>, чтобы ввести его.",
            BotMessageCatalog.Welcome("ru"));
    }

    [Fact]
    public void Welcome_Polish_ReturnsPolishText()
    {
        Assert.Equal(
            "🎄 <b>Witamy!</b>\nWyślij kod z karty lub użyj polecenia <code>/code</code>, aby go wpisać.",
            BotMessageCatalog.Welcome("pl"));
    }

    [Fact]
    public void LanguageInvalid_RussianListsPolish()
    {
        Assert.Equal(
            "🌐 Доступные языки: <code>ru</code>, <code>en</code>, <code>pl</code>.",
            BotMessageCatalog.LanguageInvalid("ru"));
    }

    [Fact]
    public void GiftActivatedTitle_EscapesHtmlInTitle()
    {
        Assert.Equal(
            "🎁 <b>Подарок активирован:</b> &lt;key&amp;gift&gt;",
            BotMessageCatalog.GiftActivatedTitle("ru", "<key&gift>"));
    }

    [Fact]
    public void ReloadConflictPrompt_IncludesEscapedGiftTitle()
    {
        Assert.Equal(
            "⚠️ <b>День 3: &lt;Новый подарок&gt;</b>\nКод этого подарка изменился. Выберите, как поступить:",
            BotMessageCatalog.ReloadConflictPrompt("ru", 3, "<Новый подарок>"));
    }
}
