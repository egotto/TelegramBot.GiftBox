using TelegramBot.GiftBox.Configuration;

namespace TelegramBot.GiftBox.Tests.Configuration;

public sealed class GiftConfigurationLoaderTests
{
    private readonly GiftConfigurationLoader _loader = new();

    [Fact]
    public async Task LoadAsync_ValidConfiguration_ReturnsCalendar()
    {
        var options = await LoadAsync(ValidConfiguration);

        Assert.Equal("Наш адвент", options.CalendarName);
        Assert.Equal(new DateOnly(2026, 12, 1), options.StartDate);
        Assert.Equal("Europe/Warsaw", options.TimeZone);
        Assert.Equal(2, options.Gifts.Count);
    }

    [Fact]
    public async Task LoadAsync_DuplicateNormalizedActivationCode_Throws()
    {
        var configuration = ValidConfiguration.Replace(
            "WINTER-02-A7M4",
            " winter-01-x9k2 ",
            StringComparison.Ordinal);

        var exception = await Assert.ThrowsAsync<GiftConfigurationException>(
            () => LoadAsync(configuration));

        Assert.Equal("Activation codes must be unique.", exception.Message);
        Assert.DoesNotContain("WINTER-01-X9K2", exception.Message);
    }

    [Fact]
    public async Task LoadAsync_RecipientAlsoConfiguredAsAdministrator_ReturnsBothRolesConfiguration()
    {
        var configuration = ValidConfiguration.Replace(
            "\"adminTelegramIds\": [987654321]",
            "\"adminTelegramIds\": [123456789]",
            StringComparison.Ordinal);

        var options = await LoadAsync(configuration);

        Assert.Contains(123456789, options.RecipientTelegramIds);
        Assert.Contains(123456789, options.AdminTelegramIds);
    }

    [Fact]
    public async Task LoadAsync_PolishDefaultLanguage_ReturnsCalendar()
    {
        var configuration = ValidConfiguration.Replace(
            "\"defaultLanguage\": \"ru\"",
            "\"defaultLanguage\": \"pl\"",
            StringComparison.Ordinal);

        var options = await LoadAsync(configuration);

        Assert.Equal("pl", options.DefaultLanguage);
    }

    [Fact]
    public async Task LoadAsync_UnsupportedGiftType_Throws()
    {
        var configuration = ValidConfiguration.Replace(
            "\"type\": \"steamLink\"",
            "\"type\": \"unknown\"",
            StringComparison.Ordinal);

        var exception = await Assert.ThrowsAsync<GiftConfigurationException>(
            () => LoadAsync(configuration));

        Assert.Contains("unsupported type", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_SurpriseTypeWithEmptyPayload_ReturnsCalendar()
    {
        var configuration = ValidConfiguration
            .Replace("\"type\": \"steamLink\"", "\"type\": \"surprise\"", StringComparison.Ordinal)
            .Replace(
                "\"url\": \"https://store.steampowered.com/app/123456/\"",
                string.Empty,
                StringComparison.Ordinal);

        var options = await LoadAsync(configuration);

        Assert.Equal("surprise", options.Gifts[0].Type);
        Assert.Equal(System.Text.Json.JsonValueKind.Object, options.Gifts[0].Payload.ValueKind);
        Assert.Empty(options.Gifts[0].Payload.EnumerateObject());
    }

    [Fact]
    public async Task LoadAsync_ExampleFile_IsValid()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../config/gifts.example.json"));

        var options = await _loader.LoadAsync(path);

        Assert.Equal(4, options.Gifts.Count);
    }

    [Fact]
    public async Task LoadAsync_NonOfficialWalletUrl_Throws()
    {
        var configuration = ValidConfiguration.Replace(
            "https://store.steampowered.com/account/redeemwalletcode",
            "https://example.com/account/redeemwalletcode",
            StringComparison.Ordinal);

        await Assert.ThrowsAsync<GiftConfigurationException>(
            () => LoadAsync(configuration));
    }

    [Fact]
    public async Task LoadAsync_ImageOutsideContentRoot_Throws()
    {
        var configuration = ValidConfiguration.Replace(
            "\"description\": \"Начинаем наше приключение.\",",
            "\"description\": \"Начинаем наше приключение.\",\n      \"image\": \"../outside.jpg\",",
            StringComparison.Ordinal);

        var exception = await Assert.ThrowsAsync<GiftConfigurationException>(
            () => LoadAsync(configuration, Path.GetTempPath()));

        Assert.Contains("content root", exception.Message, StringComparison.Ordinal);
    }

    private async Task<GiftCalendarOptions> LoadAsync(
        string configuration,
        string? contentRootPath = null)
    {
        var path = Path.Combine(Path.GetTempPath(), $"gift-calendar-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, configuration);

        try
        {
            return await _loader.LoadAsync(path, contentRootPath: contentRootPath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private const string ValidConfiguration = """
        {
          "calendarName": "Наш адвент",
          "startDate": "2026-12-01",
          "endDate": "2026-12-24",
          "timeZone": "Europe/Warsaw",
          "defaultLanguage": "ru",
          "recipientTelegramIds": [123456789],
          "adminTelegramIds": [987654321],
          "gifts": [
            {
              "id": "day01",
              "day": 1,
              "availableFrom": "2026-12-01T00:00:00+01:00",
              "activationCode": "WINTER-01-X9K2",
              "type": "steamLink",
              "title": "Первый сюрприз!",
              "description": "Начинаем наше приключение.",
              "payload": {
                "url": "https://store.steampowered.com/app/123456/"
              }
            },
            {
              "id": "day02",
              "day": 2,
              "availableFrom": "2026-12-02T00:00:00+01:00",
              "activationCode": "WINTER-02-A7M4",
              "type": "walletCode",
              "title": "Немного на новые приключения",
              "payload": {
                "code": "XXXX-XXXX-XXXX",
                "amount": 50,
                "currency": "PLN",
                "redeemUrl": "https://store.steampowered.com/account/redeemwalletcode"
              }
            }
          ]
        }
        """;
}
