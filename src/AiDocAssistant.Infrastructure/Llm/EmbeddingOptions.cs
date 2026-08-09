namespace AiDocAssistant.Infrastructure.Llm;

/// <summary>
/// Настройки провайдера эмбеддингов (OpenAI-совместимый /v1/embeddings).
/// По умолчанию — локальная Ollama с моделью bge-m3 (1024-мерный, мультиязычный,
/// хорош на русском). ApiKey нужен только для облачных провайдеров.
/// </summary>
public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "bge-m3";
    public string? ApiKey { get; set; }

    /// <summary>Размерность вектора. Должна совпадать со схемой колонки vector(N) в БД.</summary>
    public int Dimension { get; set; } = 1024;
}
