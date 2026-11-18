using System.Collections.Concurrent;
using TelegramBot.GiftBox.Application.Contracts;

namespace TelegramBot.GiftBox.Telegram;

public sealed record ReloadConversationStep(
    bool Found,
    bool IsComplete,
    ReloadCodeConflict? Conflict,
    IReadOnlyList<ReloadCodeChoice> Choices,
    string? ChoiceToken);

public sealed class ReloadConversationStore
{
    private readonly ConcurrentDictionary<long, Session> _sessions = new();

    public string Start(long adminTelegramId, IReadOnlyList<ReloadCodeConflict> conflicts)
    {
        var session = new Session(conflicts);
        _sessions[adminTelegramId] = session;
        return session.ChoiceToken;
    }

    public bool Cancel(long adminTelegramId)
    {
        return _sessions.TryRemove(adminTelegramId, out _);
    }

    public bool HasSession(long adminTelegramId)
    {
        return _sessions.ContainsKey(adminTelegramId);
    }

    public ReloadConversationStep RecordChoice(
        long adminTelegramId,
        string choiceToken,
        ActivatedGiftCodeResolution resolution)
    {
        if (!_sessions.TryGetValue(adminTelegramId, out var session))
        {
            return new ReloadConversationStep(false, false, null, [], null);
        }

        lock (session)
        {
            if (session.Index >= session.Conflicts.Count)
            {
                _sessions.TryRemove(adminTelegramId, out _);
                return new ReloadConversationStep(false, false, null, [], null);
            }

            if (!string.Equals(session.ChoiceToken, choiceToken, StringComparison.Ordinal))
            {
                return new ReloadConversationStep(false, false, null, [], null);
            }

            var currentConflict = session.Conflicts[session.Index];
            session.Choices.Add(new ReloadCodeChoice(currentConflict.GiftId, resolution));
            session.Index++;

            if (session.Index == session.Conflicts.Count)
            {
                _sessions.TryRemove(adminTelegramId, out _);
                return new ReloadConversationStep(
                    true,
                    true,
                    null,
                    session.Choices.ToArray(),
                    null);
            }

            session.ChoiceToken = CreateChoiceToken();
            return new ReloadConversationStep(
                true,
                false,
                session.Conflicts[session.Index],
                [],
                session.ChoiceToken);
        }
    }

    private static string CreateChoiceToken() => Guid.NewGuid().ToString("N");

    private sealed class Session(IReadOnlyList<ReloadCodeConflict> conflicts)
    {
        public IReadOnlyList<ReloadCodeConflict> Conflicts { get; } = conflicts;

        public List<ReloadCodeChoice> Choices { get; } = [];

        public int Index { get; set; }

        public string ChoiceToken { get; set; } = CreateChoiceToken();
    }
}
