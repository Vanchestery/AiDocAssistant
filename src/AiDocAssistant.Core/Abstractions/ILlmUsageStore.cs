using AiDocAssistant.Core.Entities;

namespace AiDocAssistant.Core.Abstractions;

public interface ILlmUsageStore
{
    Task RecordAsync(LlmUsageEvent usage, CancellationToken ct = default);
    Task<LlmUsageSummary> GetSummaryAsync(CancellationToken ct = default);
}

public sealed record LlmUsageSummary(
    int TotalCalls,
    int TotalPromptTokens,
    int TotalCompletionTokens,
    long TotalLatencyMs,
    decimal TotalEstimatedCostUsd,
    IReadOnlyList<LlmUsageByOperation> ByOperation);

public sealed record LlmUsageByOperation(
    string Operation,
    int Calls,
    int PromptTokens,
    int CompletionTokens,
    long LatencyMs,
    decimal EstimatedCostUsd);
