using System.Globalization;
using System.Text.Json;

namespace AiDocAssistant.Core.Services;

/// <summary>
/// Детерминированные eval-кейсы без LLM: reconcile + сравнение ключевых полей extraction JSON.
/// </summary>
public sealed class EvalSuiteService
{
    private static readonly Guid DocA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DocB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly DocumentReconcileService _reconcile;

    public EvalSuiteService(DocumentReconcileService reconcile) => _reconcile = reconcile;

    public EvalSuiteResult RunAll()
    {
        var cases = new List<EvalCaseResult>();
        cases.AddRange(RunReconcileEvals());
        cases.AddRange(RunExtractionFieldEvals());
        return new EvalSuiteResult(cases, cases.All(c => c.Passed));
    }

    private IEnumerable<EvalCaseResult> RunReconcileEvals()
    {
        const string matchingJson =
            """
            {
              "doc_type": "счет",
              "number": "2026-041",
              "date": "2026-07-15",
              "counterparty": { "name": "ООО Тест", "inn": "7701234567" },
              "items": [ { "name": "товар", "quantity": 1, "amount": 1000.0 } ],
              "total_amount": 1000.0,
              "vat_amount": 166.67,
              "currency": "RUB",
              "confidence": 0.9
            }
            """;

        var matching = _reconcile.Reconcile(
        [
            new DocumentExtractionSnapshot(DocA, "a.pdf", matchingJson),
            new DocumentExtractionSnapshot(DocB, "b.pdf", matchingJson)
        ]);

        yield return new EvalCaseResult(
            "reconcile.matching_documents",
            matching.HasDiscrepancies == false,
            matching.HasDiscrepancies ? "ожидали 0 расхождений" : null);

        var mismatch = _reconcile.Reconcile(
        [
            new DocumentExtractionSnapshot(DocA, "a.pdf", SampleJson(total: 112700)),
            new DocumentExtractionSnapshot(DocB, "b.pdf", SampleJson(total: 115000))
        ]);

        yield return new EvalCaseResult(
            "reconcile.total_amount_mismatch",
            mismatch.HasDiscrepancies && mismatch.Discrepancies.Any(d => d.Field == "total_amount"),
            mismatch.HasDiscrepancies ? null : "ожидали расхождение total_amount");
    }

    private static IEnumerable<EvalCaseResult> RunExtractionFieldEvals()
    {
        yield return CompareExtractionFields(
            "extraction.invoice_a_fields",
            SampleJson(total: 112700),
            expectedNumber: "2026-041",
            expectedTotal: 112700m);

        yield return CompareExtractionFields(
            "extraction.invoice_b_fields",
            SampleJson(total: 115000),
            expectedNumber: "2026-041",
            expectedTotal: 115000m);
    }

    private static EvalCaseResult CompareExtractionFields(
        string name,
        string json,
        string expectedNumber,
        decimal expectedTotal)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var number = root.GetProperty("number").GetString();
        var total = root.GetProperty("total_amount").GetDecimal();

        var passed = number == expectedNumber && total == expectedTotal;
        var detail = passed
            ? null
            : $"number={number}, total={total.ToString(CultureInfo.InvariantCulture)}";

        return new EvalCaseResult(name, passed, detail);
    }

    private static string SampleJson(decimal total) =>
        $$"""
          {
            "doc_type": "счет",
            "number": "2026-041",
            "date": "2026-07-15",
            "counterparty": { "name": "OOO KofePoint", "inn": "7707654321" },
            "items": [ { "name": "товар", "quantity": 1, "amount": {{total.ToString(CultureInfo.InvariantCulture)}} } ],
            "total_amount": {{total.ToString(CultureInfo.InvariantCulture)}},
            "vat_amount": 18783.33,
            "currency": "RUB",
            "confidence": 0.9
          }
          """;
}

public sealed record EvalCaseResult(string Name, bool Passed, string? Detail);

public sealed record EvalSuiteResult(
    IReadOnlyList<EvalCaseResult> Cases,
    bool AllPassed);
