namespace TelegramBot.GiftBox.Configuration;

public interface IGiftCalendarProvider
{
    GiftCalendarOptions Current { get; }

    void Replace(GiftCalendarOptions options);
}

public sealed class GiftCalendarProvider : IGiftCalendarProvider
{
    private GiftCalendarOptions _current;

    public GiftCalendarProvider(GiftCalendarOptions initialOptions)
    {
        _current = initialOptions;
    }

    public GiftCalendarOptions Current => Volatile.Read(ref _current);

    public void Replace(GiftCalendarOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Volatile.Write(ref _current, options);
    }
}
