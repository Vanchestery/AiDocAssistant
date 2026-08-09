using AiDocAssistant.Core.Entities;

namespace AiDocAssistant.Core.Abstractions;

/// <summary>Хранение сессий чата и истории сообщений.</summary>
public interface IChatSessionStore
{
    Task<Guid> CreateSessionAsync(CancellationToken ct = default);

    Task<bool> SessionExistsAsync(Guid sessionId, CancellationToken ct = default);

    Task<ChatSession?> GetSessionWithMessagesAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>Последние N сообщений в хронологическом порядке (для контекста LLM).</summary>
    Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(Guid sessionId, int limit, CancellationToken ct = default);

    Task AddExchangeAsync(
        Guid sessionId,
        string userContent,
        string assistantContent,
        IReadOnlyList<ChatCitation> citations,
        string model,
        int promptTokens,
        int completionTokens,
        CancellationToken ct = default);
}
