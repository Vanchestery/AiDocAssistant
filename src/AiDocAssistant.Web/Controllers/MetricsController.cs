using AiDocAssistant.Core.Abstractions;
using AiDocAssistant.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiDocAssistant.Web.Controllers;

[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private readonly MetricsService _metrics;

    public MetricsController(MetricsService metrics) => _metrics = metrics;

    /// <summary>Сводка: LLM-телеметрия, счётчики БД, eval-кейсы.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<MetricsSummaryDto>> GetSummary(CancellationToken ct)
    {
        var summary = await _metrics.GetSummaryAsync(ct);
        return MetricsSummaryDto.FromModel(summary);
    }

    /// <summary>Детерминированные eval-кейсы (reconcile + поля extraction JSON).</summary>
    [HttpGet("evals")]
    public ActionResult<EvalSuiteDto> GetEvals()
    {
        var evals = _metrics.RunEvals();
        return EvalSuiteDto.FromModel(evals);
    }
}

public record MetricsSummaryDto(
    LlmUsageSummaryDto Llm,
    DataCountsDto Data,
    EvalSuiteDto Evals)
{
    public static MetricsSummaryDto FromModel(MetricsSummary summary) =>
        new(
            LlmUsageSummaryDto.FromModel(summary.Llm),
            DataCountsDto.FromModel(summary.Data),
            EvalSuiteDto.FromModel(summary.Evals));
}

public record LlmUsageSummaryDto(
    int TotalCalls,
    int TotalPromptTokens,
    int TotalCompletionTokens,
    long TotalLatencyMs,
    decimal TotalEstimatedCostUsd,
    IReadOnlyList<LlmUsageByOperationDto> ByOperation)
{
    public static LlmUsageSummaryDto FromModel(LlmUsageSummary summary) =>
        new(
            summary.TotalCalls,
            summary.TotalPromptTokens,
            summary.TotalCompletionTokens,
            summary.TotalLatencyMs,
            summary.TotalEstimatedCostUsd,
            summary.ByOperation.Select(LlmUsageByOperationDto.FromModel).ToList());
}

public record LlmUsageByOperationDto(
    string Operation,
    int Calls,
    int PromptTokens,
    int CompletionTokens,
    long LatencyMs,
    decimal EstimatedCostUsd)
{
    public static LlmUsageByOperationDto FromModel(LlmUsageByOperation row) =>
        new(row.Operation, row.Calls, row.PromptTokens, row.CompletionTokens, row.LatencyMs, row.EstimatedCostUsd);
}

public record DataCountsDto(
    int Documents,
    int ExtractedDocuments,
    int ChatSessions,
    int AgentTasks,
    int LlmUsageEvents)
{
    public static DataCountsDto FromModel(DataCountsSnapshot counts) =>
        new(counts.Documents, counts.ExtractedDocuments, counts.ChatSessions, counts.AgentTasks, counts.LlmUsageEvents);
}

public record EvalSuiteDto(bool AllPassed, IReadOnlyList<EvalCaseDto> Cases)
{
    public static EvalSuiteDto FromModel(EvalSuiteResult result) =>
        new(result.AllPassed, result.Cases.Select(EvalCaseDto.FromModel).ToList());
}

public record EvalCaseDto(string Name, bool Passed, string? Detail)
{
    public static EvalCaseDto FromModel(EvalCaseResult result) =>
        new(result.Name, result.Passed, result.Detail);
}
