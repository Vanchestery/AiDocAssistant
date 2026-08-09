namespace AiDocAssistant.Core.Services;

/// <summary>Параметры RAG-чата: сколько чанков искать и сколько истории передавать в LLM.</summary>
public sealed class RagOptions
{
    public const string SectionName = "Rag";

    /// <summary>Число ближайших чанков из pgvector (top-K).</summary>
    public int TopK { get; set; } = 5;

    /// <summary>Последние N сообщений сессии в промпт (пары user/assistant).</summary>
    public int MaxHistoryMessages { get; set; } = 10;
}
