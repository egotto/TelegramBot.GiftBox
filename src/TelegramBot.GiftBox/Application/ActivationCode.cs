using System.Text.RegularExpressions;

namespace TelegramBot.GiftBox.Application;

public static partial class ActivationCode
{
    public static string Normalize(string activationCode)
    {
        ArgumentNullException.ThrowIfNull(activationCode);

        var normalizedCode = activationCode.Trim().ToUpperInvariant();
        if (!ValidCodePattern().IsMatch(normalizedCode))
        {
            throw new ArgumentException("Activation-code format is invalid.", nameof(activationCode));
        }

        return normalizedCode;
    }

    [GeneratedRegex("^[A-Z0-9_-]{8,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidCodePattern();
}
