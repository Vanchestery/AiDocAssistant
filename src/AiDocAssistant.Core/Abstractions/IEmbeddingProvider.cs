namespace AiDocAssistant.Core.Abstractions;

/// <summary>
/// Model-agnostic генерация эмбеддингов. Реализация — OpenAI-совместимый эндпоинт
/// (по умолчанию локальная Ollama), но за интерфейсом можно подменить провайдера.
/// Возвращаем float[] — без привязки к типу Vector из pgvector (см. DECISIONS.md №13).
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>Размерность вектора модели (должна совпадать со схемой колонки vector(N)).</summary>
    int Dimension { get; }

    /// <summary>Эмбеддинги для батча текстов, в том же порядке, что и вход.</summary>
    Task<IReadOnlyList<float[]>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken ct = default);
}
