namespace TelegramBot.GiftBox.Configuration;

public sealed class GiftConfigurationException : Exception
{
    public GiftConfigurationException(string message)
        : base(message)
    {
    }

    public GiftConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
