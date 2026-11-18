using TelegramBot.GiftBox.Application;

namespace TelegramBot.GiftBox.Tests.Application;

public sealed class ActivationCodeTests
{
    [Fact]
    public void Normalize_TrimsAndUppercasesCode()
    {
        Assert.Equal("WINTER-01-X9K2", ActivationCode.Normalize("  winter-01-x9k2  "));
    }

    [Theory]
    [InlineData("SHORT")]
    [InlineData("NOT VALID-123")]
    [InlineData("WINTER-01-X9K2!")]
    public void Normalize_RejectsInvalidCode(string code)
    {
        Assert.Throws<ArgumentException>(() => ActivationCode.Normalize(code));
    }
}
