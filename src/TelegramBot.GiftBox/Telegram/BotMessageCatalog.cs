namespace TelegramBot.GiftBox.Telegram;

using System.Net;
using TelegramBot.GiftBox.Configuration;

public static class BotMessageCatalog
{
    public static string Welcome(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "🎄 <b>Добро пожаловать!</b>\nОтправьте код с карточки или используйте <code>/code</code>, чтобы ввести его.",
        LanguageCodes.Polish => "🎄 <b>Witamy!</b>\nWyślij kod z karty lub użyj polecenia <code>/code</code>, aby go wpisać.",
        _ => "🎄 <b>Welcome!</b>\nSend the activation code from your scratch card, or use <code>/code</code> to enter it."
    };

    public static string CodePrompt(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "🔐 <b>Введите код активации</b> следующим сообщением.",
        LanguageCodes.Polish => "🔐 <b>Wpisz kod aktywacyjny</b> w następnej wiadomości.",
        _ => "🔐 <b>Enter the activation code</b> in your next message."
    };

    public static string AccessDenied(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "⛔ Доступ запрещён.",
        LanguageCodes.Polish => "⛔ Brak dostępu.",
        _ => "⛔ Access denied."
    };

    public static string InvalidCode(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "❔ Не удалось активировать этот код. Проверьте его и попробуйте ещё раз.",
        LanguageCodes.Polish => "❔ Nie udało się aktywować tego kodu. Sprawdź go i spróbuj ponownie.",
        _ => "❔ That code could not be activated. Check it and try again."
    };

    public static string GiftUnavailable(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "🕒 Этот подарок пока недоступен.",
        LanguageCodes.Polish => "🕒 Ten prezent nie jest jeszcze dostępny.",
        _ => "🕒 This gift is not available yet."
    };

    public static string AlreadyClaimed(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "🎁 Этот подарок уже активирован.",
        LanguageCodes.Polish => "🎁 Ten prezent został już aktywowany.",
        _ => "🎁 This gift has already been claimed."
    };

    public static string Activated(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "🎁 Подарок активирован!",
        LanguageCodes.Polish => "🎁 Prezent aktywowany!",
        _ => "🎁 Gift activated!"
    };

    public static string CalendarEmpty(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "📅 Календарь пока недоступен.",
        LanguageCodes.Polish => "📅 Kalendarz jest jeszcze niedostępny.",
        _ => "📅 The calendar is not available yet."
    };

    public static string CalendarDay(
        string languageCode,
        int day,
        bool isActivated,
        bool isAvailable,
        bool isUnlockedEarly,
        string? title)
    {
        var state = languageCode switch
        {
            LanguageCodes.Russian => isActivated
                ? "✅ открыт"
                : isUnlockedEarly
                    ? "🔓 открыт досрочно"
                    : isAvailable
                        ? "🎁 доступен"
                        : "🔒 пока закрыт",
                LanguageCodes.Polish => isActivated
                    ? "✅ otwarty"
                    : isUnlockedEarly
                        ? "🔓 odblokowany wcześniej"
                        : isAvailable
                            ? "🎁 dostępny"
                            : "🔒 jeszcze niedostępny",
            _ => isActivated
                ? "✅ opened"
                : isUnlockedEarly
                    ? "🔓 unlocked early"
                    : isAvailable
                        ? "🎁 available"
                        : "🔒 not available yet"
        };

        var label = languageCode switch
        {
            LanguageCodes.Russian => $"📅 <b>День {day}:</b> {state}",
            LanguageCodes.Polish => $"📅 <b>Dzień {day}:</b> {state}",
            _ => $"📅 <b>Day {day}:</b> {state}"
        };
        return title is null ? label : $"{label} — {WebUtility.HtmlEncode(title)}";
    }

    public static string AdminStatus(string languageCode, int total, int activated) => languageCode switch
    {
        LanguageCodes.Russian => $"📊 <b>Активировано:</b> {activated} из {total}.",
        LanguageCodes.Polish => $"📊 <b>Aktywowano:</b> {activated} z {total}.",
        _ => $"📊 <b>Activated:</b> {activated} of {total}."
    };

    public static string UnlockSucceeded(string languageCode, int day) => languageCode switch
    {
        LanguageCodes.Russian => $"🔓 День {day} разблокирован.",
        LanguageCodes.Polish => $"🔓 Dzień {day} został odblokowany.",
        _ => $"🔓 Day {day} is now unlocked."
    };

    public static string UnlockNotFound(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "⚠️ Для этого дня нет активного подарка.",
        LanguageCodes.Polish => "⚠️ Brak aktywnego prezentu na ten dzień.",
        _ => "⚠️ No active gift exists for that day."
    };

    public static string ReloadSucceeded(string languageCode, int count) => languageCode switch
    {
        LanguageCodes.Russian => $"✅ <b>Календарь обновлён.</b> Подарков в конфигурации: {count}.",
        LanguageCodes.Polish => $"✅ <b>Kalendarz zaktualizowany.</b> Liczba prezentów w konfiguracji: {count}.",
        _ => $"✅ <b>Calendar updated.</b> Gifts in configuration: {count}."
    };

    public static string ReloadConflictPrompt(string languageCode, int day, string title) => languageCode switch
    {
        LanguageCodes.Russian => $"⚠️ <b>День {day}: {WebUtility.HtmlEncode(title)}</b>\nКод этого подарка изменился. Выберите, как поступить:",
        LanguageCodes.Polish => $"⚠️ <b>Dzień {day}: {WebUtility.HtmlEncode(title)}</b>\nKod aktywacyjny tego prezentu uległ zmianie. Wybierz, co zrobić:",
        _ => $"⚠️ <b>Day {day}: {WebUtility.HtmlEncode(title)}</b>\nThe activation code for this gift changed. Choose how to proceed:"
    };

    public static string ReloadAddButton(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "Добавить код",
        LanguageCodes.Polish => "Dodaj kod",
        _ => "Add code"
    };

    public static string ReloadReplaceButton(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "Заменить код",
        LanguageCodes.Polish => "Zastąp kod",
        _ => "Replace code"
    };

    public static string ReloadInvalidChoice(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "⌨️ Нажмите одну из кнопок или используйте <code>/cancel</code>, чтобы отменить загрузку.",
        LanguageCodes.Polish => "⌨️ Kliknij jeden z przycisków lub użyj <code>/cancel</code>, aby anulować wczytywanie.",
        _ => "⌨️ Press one of the buttons, or use <code>/cancel</code> to stop the reload."
    };

    public static string ReloadCancelled(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "↩️ Обновление конфигурации отменено.",
        LanguageCodes.Polish => "↩️ Aktualizacja konfiguracji została anulowana.",
        _ => "↩️ Configuration reload cancelled."
    };

    public static string ReloadFailed(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "⚠️ <b>Не удалось применить конфигурацию.</b> Частичных изменений нет. Проверьте файл и попробуйте ещё раз.",
        LanguageCodes.Polish => "⚠️ <b>Nie udało się zastosować konfiguracji.</b> Nie wprowadzono częściowych zmian. Sprawdź plik i spróbuj ponownie.",
        _ => "⚠️ <b>The configuration could not be applied.</b> No partial update was made. Check the configuration and try again."
    };

    public static string UnexpectedError(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "⚠️ Что-то пошло не так. Попробуйте ещё раз позже.",
        LanguageCodes.Polish => "⚠️ Coś poszło nie tak. Spróbuj ponownie później.",
        _ => "⚠️ Something went wrong. Please try again later."
    };

    public static string UnlockUsage(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "🛠 Использование: <code>/unlock &lt;день&gt;</code>",
        LanguageCodes.Polish => "🛠 Użycie: <code>/unlock &lt;dzień&gt;</code>",
        _ => "🛠 Usage: <code>/unlock &lt;day&gt;</code>"
    };

    public static string LanguagePrompt(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "🌐 Выберите язык командой <code>/language ru</code> или <code>/language en</code>.",
        LanguageCodes.Polish => "🌐 Wybierz język poleceniem <code>/language ru</code>, <code>/language en</code> lub <code>/language pl</code>.",
        _ => "🌐 Choose a language with <code>/language ru</code>, <code>/language en</code>, or <code>/language pl</code>."
    };

    public static string LanguageUpdated(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "🌐 Язык установлен: <b>русский</b>.",
        LanguageCodes.Polish => "🌐 Ustawiono język: <b>polski</b>.",
        _ => "🌐 Language set to <b>English</b>."
    };

    public static string LanguageInvalid(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "🌐 Доступные языки: <code>ru</code>, <code>en</code>, <code>pl</code>.",
        LanguageCodes.Polish => "🌐 Dostępne języki: <code>ru</code>, <code>en</code>, <code>pl</code>.",
        _ => "🌐 Supported languages: <code>ru</code>, <code>en</code>, <code>pl</code>."
    };

    public static string GiftCodeLabel(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "🔑 Код",
        LanguageCodes.Polish => "🔑 Kod",
        _ => "🔑 Code"
    };

    public static string RedeemWallet(string languageCode) => languageCode switch
    {
        LanguageCodes.Russian => "↗️ Активировать в Steam",
        LanguageCodes.Polish => "↗️ Aktywuj na Steam",
        _ => "↗️ Redeem on Steam"
    };

    public static string GiftActivatedTitle(string languageCode, string title) => languageCode switch
    {
        LanguageCodes.Russian => $"🎁 <b>Подарок активирован:</b> {WebUtility.HtmlEncode(title)}",
        LanguageCodes.Polish => $"🎁 <b>Prezent aktywowany:</b> {WebUtility.HtmlEncode(title)}",
        _ => $"🎁 <b>Gift activated:</b> {WebUtility.HtmlEncode(title)}"
    };
}
