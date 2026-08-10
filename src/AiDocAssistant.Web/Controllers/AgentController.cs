using System.Text.Json;
using AiDocAssistant.Core.Abstractions;
using AiDocAssistant.Core.Entities;
using AiDocAssistant.Core.Services;
using AiDocAssistant.Infrastructure.Agent;
using Microsoft.AspNetCore.Mvc;

namespace AiDocAssistant.Web.Controllers;

[ApiController]
[Route("api/agent")]
public class AgentController : ControllerBase
{
    private readonly AgentTaskService _tasks;
    private readonly AgentToolRegistry _tools;
    private readonly IFileStorage _storage;

    public AgentController(AgentTaskService tasks, AgentToolRegistry tools, IFileStorage storage)
    {
        _tasks = tasks;
        _tools = tools;
        _storage = storage;
    }

    /// <summary>Доступные tools (явный режим Фазы 3).</summary>
    [HttpGet("tools")]
    public ActionResult<IReadOnlyList<string>> ListTools() =>
        _tools.Names.OrderBy(n => n).ToList();

    /// <summary>Запустить tool над документами.</summary>
    [HttpPost("tasks")]
    [ProducesResponseType(typeof(AgentTaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AgentTaskDto), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgentTaskDto>> RunTask(
        [FromBody] RunAgentTaskRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Tool))
            return BadRequest("Укажите tool (reconcile, summarize, generate_report).");

        if (request.DocumentIds is null || request.DocumentIds.Count == 0)
            return BadRequest("Укажите documentIds.");

        try
        {
            var action = await _tasks.RunAsync(request.Tool, request.DocumentIds, ct);
            var dto = AgentTaskDto.FromEntity(action);
            return action.Status == AgentActionStatus.Failed
                ? UnprocessableEntity(dto)
                : CreatedAtAction(nameof(GetTask), new { id = action.Id }, dto);
        }
        catch (KeyNotFoundException e)
        {
            return BadRequest(e.Message);
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("tasks/{id:guid}")]
    [ProducesResponseType(typeof(AgentTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentTaskDto>> GetTask(Guid id, CancellationToken ct)
    {
        var action = await _tasks.GetAsync(id, ct);
        return action is null ? NotFound() : AgentTaskDto.FromEntity(action);
    }

    /// <summary>Скачать xlsx-отчёт задачи generate_report.</summary>
    [HttpGet("tasks/{id:guid}/report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadReport(Guid id, CancellationToken ct)
    {
        var action = await _tasks.GetAsync(id, ct);
        if (action is null
            || action.Tool != AgentToolNames.GenerateReport
            || action.Status != AgentActionStatus.Completed
            || string.IsNullOrWhiteSpace(action.ResultJson))
        {
            return NotFound();
        }

        using var doc = JsonDocument.Parse(action.ResultJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("storagePath", out var pathEl)
            || !root.TryGetProperty("fileName", out var nameEl))
        {
            return NotFound();
        }

        var storagePath = pathEl.GetString();
        if (string.IsNullOrWhiteSpace(storagePath))
            return NotFound();

        var fullPath = _storage.GetFullPath(storagePath);
        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        var fileName = nameEl.GetString() ?? "report.xlsx";
        return PhysicalFile(
            fullPath,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}

public record RunAgentTaskRequest(string Tool, IReadOnlyList<Guid> DocumentIds);

public record AgentTaskDto(
    Guid Id,
    string Tool,
    string Status,
    string InputJson,
    string? ResultJson,
    string? Error,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt)
{
    public static AgentTaskDto FromEntity(AgentAction action) =>
        new(
            action.Id,
            action.Tool,
            action.Status.ToString(),
            action.InputJson,
            action.ResultJson,
            action.Error,
            action.CreatedAt,
            action.CompletedAt);
}
