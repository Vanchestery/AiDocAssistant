namespace AiDocAssistant.Core.Abstractions;

/// <summary>
/// Model-agnostic доступ к LLM. Реализации: DeepSeek (Infrastructure),
/// в перспективе — Ollama или любой другой OpenAI-совместимый провайдер.
/// </summary>
public interface ILlmProvider
{
    Task<LlmCompletion> CompleteAsync(LlmRequest request, CancellationToken ct = default);
}

public sealed record LlmMessage(string Role, string Content)
{
    public static LlmMessage System(string content) => new("system", content);
    public static LlmMessage User(string content) => new("user", content);
    public static LlmMessage Assistant(string content) => new("assistant", content);
}

public sealed record LlmRequest(
    IReadOnlyList<LlmMessage> Messages,
    bool JsonMode = false,
    double Temperature = 0.2,
    int? MaxTokens = null);

/// <summary>Ответ + телеметрия (токены нужны для метрик стоимости, Фаза 4).</summary>
public sealed record LlmCompletion(
    string Content,
    string Model,
    int PromptTokens,
    int CompletionTokens);
