namespace AiDocAssistant.Core.Abstractions;

/// <summary>Инструмент агента (Фаза 3). Каждый tool — отдельная реализация в Infrastructure.</summary>
public interface IAgentTool
{
    /// <summary>Имя для явного вызова через API: reconcile, summarize, generate_report.</summary>
    string Name { get; }

    Task<AgentToolResult> ExecuteAsync(AgentToolInput input, CancellationToken ct = default);
}

public sealed record AgentToolInput(IReadOnlyList<Guid> DocumentIds, string? ParametersJson = null);

public sealed record AgentToolResult(string ResultJson, string? Message = null);
