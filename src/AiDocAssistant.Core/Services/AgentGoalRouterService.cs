using System.Text;
using System.Text.Json;
using AiDocAssistant.Core.Abstractions;
using AiDocAssistant.Core.Llm;
using AiDocAssistant.Core.Agent;

namespace AiDocAssistant.Core.Services;

/// <summary>
/// Выбор tool по текстовой цели пользователя — один LLM-вызов в JSON-mode.
/// Без native tool-calling API: модель возвращает { tool, reasoning }.
/// </summary>
public sealed class AgentGoalRouterService
{
    private const string SystemPrompt =
        """
        Ты — маршрутизатор задач бэк-офиса. По цели пользователя выбери ОДИН tool из списка.
        Верни СТРОГО один json-объект: { "tool": "...", "reasoning": "..." }.
        reasoning — одно короткое предложение на русском, почему выбран этот tool.

        Доступные tools:
        - reconcile — сверка полей между документами, поиск расхождений (нужно минимум 2 документа)
        - summarize — краткая текстовая сводка по документам (1+ документов)
        - generate_report — xlsx-отчёт по документам (1+ документов)

        Правила: tool — только одно из трёх имён; не выдумывай другие tools.
        """;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILlmProvider _llm;

    public AgentGoalRouterService(ILlmProvider llm) => _llm = llm;

    public async Task<GoalRoutingOutcome> RouteAsync(
        string goal,
        int documentCount,
        IReadOnlyCollection<string> availableTools,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(goal))
            throw new ArgumentException("Цель не указана.", nameof(goal));

        if (documentCount == 0)
            throw new ArgumentException("Нужен хотя бы один documentId.");

        var userPrompt = BuildUserPrompt(goal.Trim(), documentCount, availableTools);

        var completion = await _llm.CompleteAsync(
            new LlmRequest(
            [
                LlmMessage.System(SystemPrompt),
                LlmMessage.User(userPrompt)
            ],
            JsonMode: true,
            Temperature: 0.0,
            Operation: LlmOperations.GoalRouter),
            ct);

        var routing = ParseRouting(completion.Content);
        ValidateRouting(routing, availableTools, documentCount);

        return new GoalRoutingOutcome(
            routing.Tool,
            routing.Reasoning.Trim(),
            completion.Model,
            completion.PromptTokens,
            completion.CompletionTokens);
    }

    private static string BuildUserPrompt(
        string goal,
        int documentCount,
        IReadOnlyCollection<string> availableTools)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Цель пользователя: {goal}");
        sb.AppendLine($"Количество documentIds: {documentCount}");
        sb.AppendLine($"Доступные tools: {string.Join(", ", availableTools.OrderBy(t => t))}");
        sb.AppendLine();
        sb.AppendLine("Выбери tool и верни json.");
        return sb.ToString();
    }

    private static GoalRoutingResponse ParseRouting(string content)
    {
        try
        {
            var routing = JsonSerializer.Deserialize<GoalRoutingResponse>(content, JsonOpts)
                ?? throw new InvalidOperationException("Пустой JSON маршрутизации.");

            if (string.IsNullOrWhiteSpace(routing.Tool))
                throw new InvalidOperationException("Поле tool отсутствует в ответе LLM.");

            if (string.IsNullOrWhiteSpace(routing.Reasoning))
                routing.Reasoning = "Tool выбран по формулировке цели.";

            return routing;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"LLM вернул некорректный JSON маршрутизации: {ex.Message}");
        }
    }

    private static void ValidateRouting(
        GoalRoutingResponse routing,
        IReadOnlyCollection<string> availableTools,
        int documentCount)
    {
        var tool = routing.Tool.Trim();
        if (!availableTools.Contains(tool, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"LLM выбрал неизвестный tool «{tool}». Доступны: {string.Join(", ", availableTools)}.");
        }

        routing.Tool = availableTools.First(t => t.Equals(tool, StringComparison.OrdinalIgnoreCase));

        if (routing.Tool == AgentToolNames.Reconcile && documentCount < 2)
        {
            throw new InvalidOperationException(
                "Для сверки (reconcile) нужно минимум 2 documentId. Добавьте документы или измените цель.");
        }
    }

    private sealed class GoalRoutingResponse
    {
        public string Tool { get; set; } = null!;
        public string Reasoning { get; set; } = null!;
    }
}

public sealed record GoalRoutingOutcome(
    string Tool,
    string Reasoning,
    string Model,
    int PromptTokens,
    int CompletionTokens);
