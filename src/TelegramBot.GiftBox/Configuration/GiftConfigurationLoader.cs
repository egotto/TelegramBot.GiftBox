using System.Text.Json;
using System.Text.Json.Serialization;
using TelegramBot.GiftBox.Application;

namespace TelegramBot.GiftBox.Configuration;

public sealed partial class GiftConfigurationLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public async Task<GiftCalendarOptions> LoadAsync(
        string path,
        CancellationToken cancellationToken = default,
        string? contentRootPath = null)
    {
        string json;
        try
        {
            json = await File.ReadAllTextAsync(path, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new GiftConfigurationException("Gift configuration could not be read.", exception);
        }

        GiftCalendarOptions options;
        try
        {
            options = JsonSerializer.Deserialize<GiftCalendarOptions>(json, SerializerOptions)
                ?? throw new GiftConfigurationException("Gift configuration is empty.");
        }
        catch (JsonException exception)
        {
            throw new GiftConfigurationException(
                "Gift configuration is not valid JSON or does not match the expected schema.",
                exception);
        }

        Validate(options, contentRootPath ?? Directory.GetCurrentDirectory());
        return options;
    }

    private static void Validate(GiftCalendarOptions options, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(options.CalendarName))
        {
            throw new GiftConfigurationException("Calendar name must not be empty.");
        }

        if (options.StartDate > options.EndDate)
        {
            throw new GiftConfigurationException("Calendar start date must not be after its end date.");
        }

        if (string.IsNullOrWhiteSpace(options.TimeZone))
        {
            throw new GiftConfigurationException("Calendar time zone must not be empty.");
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new GiftConfigurationException("Calendar time zone is not recognized.", exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new GiftConfigurationException("Calendar time zone is invalid.", exception);
        }

        if (!LanguageCodes.IsSupported(options.DefaultLanguage))
        {
            throw new GiftConfigurationException("Default language must be 'ru' or 'en'.");
        }

        if (options.RecipientTelegramIds is null || options.AdminTelegramIds is null)
        {
            throw new GiftConfigurationException("Recipient and administrator Telegram IDs are required.");
        }

        ValidateTelegramIds(options.RecipientTelegramIds, "recipient");
        ValidateTelegramIds(options.AdminTelegramIds, "administrator");

        if (options.Gifts is null || options.Gifts.Count == 0)
        {
            throw new GiftConfigurationException("At least one gift must be configured.");
        }

        var calendarDayCount = options.EndDate.DayNumber - options.StartDate.DayNumber + 1;
        var giftIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var giftDays = new HashSet<int>();
        var normalizedCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var gift in options.Gifts)
        {
            ValidateGift(gift, calendarDayCount, giftIds, giftDays, normalizedCodes, contentRootPath);
        }
    }

    private static void ValidateTelegramIds(IReadOnlyCollection<long> telegramIds, string role)
    {
        if (telegramIds.Count == 0 || telegramIds.Any(id => id <= 0))
        {
            throw new GiftConfigurationException(
                $"At least one positive Telegram ID must be configured for {role}s.");
        }

        if (telegramIds.Distinct().Count() != telegramIds.Count)
        {
            throw new GiftConfigurationException($"{role} Telegram IDs must be unique.");
        }
    }

    private static void ValidateGift(
        GiftDefinition gift,
        int calendarDayCount,
        ISet<string> giftIds,
        ISet<int> giftDays,
        ISet<string> normalizedCodes,
        string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(gift.Id)
            || gift.Id.Length > 100
            || !giftIds.Add(gift.Id))
        {
            throw new GiftConfigurationException("Gift IDs must be non-empty and unique.");
        }

        if (gift.Day < 1 || gift.Day > calendarDayCount || !giftDays.Add(gift.Day))
        {
            throw new GiftConfigurationException(
                "Gift days must be unique and fall within the configured calendar range.");
        }

        if (gift.ActivationCode is null)
        {
            throw new GiftConfigurationException($"Gift '{gift.Id}' must have an activation code.");
        }

        string normalizedCode;
        try
        {
            normalizedCode = ActivationCode.Normalize(gift.ActivationCode);
        }
        catch (ArgumentException)
        {
            throw new GiftConfigurationException(
                "Activation codes must contain 8 to 64 letters, digits, hyphens, or underscores.");
        }

        if (!normalizedCodes.Add(normalizedCode))
        {
            throw new GiftConfigurationException("Activation codes must be unique.");
        }

        if (string.IsNullOrWhiteSpace(gift.Title) || gift.Title.Length > 300)
        {
            throw new GiftConfigurationException($"Gift '{gift.Id}' must have a title no longer than 300 characters.");
        }

        ValidateImage(gift, contentRootPath);

        if (gift.Payload.ValueKind != JsonValueKind.Object)
        {
            throw new GiftConfigurationException($"Gift '{gift.Id}' payload must be a JSON object.");
        }

        switch (gift.Type)
        {
            case GiftTypes.SteamCode:
                RequireString(gift, "code");
                ValidateOptionalHttpsUrl(gift, "instructionsUrl");
                break;
            case GiftTypes.SteamLink:
                RequireHttpsUrl(gift, "url");
                break;
            case GiftTypes.WalletCode:
                RequireString(gift, "code");
                RequireHttpsUrl(gift, "redeemUrl");
                ValidateWalletRedemptionUrl(gift);
                break;
            case GiftTypes.Surprise:
                break;
            default:
                throw new GiftConfigurationException($"Gift '{gift.Id}' has an unsupported type.");
        }
    }

    private static void RequireString(GiftDefinition gift, string propertyName)
    {
        if (!gift.Payload.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new GiftConfigurationException(
                $"Gift '{gift.Id}' payload must contain a non-empty '{propertyName}' value.");
        }
    }

    private static void RequireHttpsUrl(GiftDefinition gift, string propertyName)
    {
        if (!gift.Payload.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String
            || !Uri.TryCreate(property.GetString(), UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new GiftConfigurationException(
                $"Gift '{gift.Id}' payload must contain a valid HTTPS '{propertyName}' URL.");
        }
    }

    private static void ValidateOptionalHttpsUrl(GiftDefinition gift, string propertyName)
    {
        if (!gift.Payload.TryGetProperty(propertyName, out var property))
        {
            return;
        }

        if (property.ValueKind != JsonValueKind.String
            || !Uri.TryCreate(property.GetString(), UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new GiftConfigurationException(
                $"Gift '{gift.Id}' payload must contain a valid HTTPS '{propertyName}' URL.");
        }
    }

    private static void ValidateWalletRedemptionUrl(GiftDefinition gift)
    {
        var uri = new Uri(gift.Payload.GetProperty("redeemUrl").GetString()!);
        if (!uri.Host.Equals("store.steampowered.com", StringComparison.OrdinalIgnoreCase)
            || !uri.AbsolutePath.TrimEnd('/').Equals(
                "/account/redeemwalletcode",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new GiftConfigurationException(
                $"Gift '{gift.Id}' must use the official Steam Wallet redemption URL.");
        }
    }

    private static void ValidateImage(GiftDefinition gift, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(gift.Image))
        {
            return;
        }

        var rootPath = Path.GetFullPath(contentRootPath);
        var imagePath = Path.GetFullPath(gift.Image, rootPath);
        var rootPrefix = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;
        if (!imagePath.StartsWith(rootPrefix, StringComparison.Ordinal)
            || !File.Exists(imagePath))
        {
            throw new GiftConfigurationException(
                $"Gift '{gift.Id}' image must exist inside the application content root.");
        }
    }

}
