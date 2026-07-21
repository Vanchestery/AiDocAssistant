using AiDocAssistant.Core.Abstractions;
using AiDocAssistant.Core.Entities;
using AiDocAssistant.Core.Services;
using AiDocAssistant.Infrastructure.Parsing;
using AiDocAssistant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiDocAssistant.Web.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly CompositeDocumentParser _parser;
    private readonly DocumentExtractionService _extraction;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        AppDbContext db,
        IFileStorage storage,
        CompositeDocumentParser parser,
        DocumentExtractionService extraction,
        ILogger<DocumentsController> logger)
    {
        _db = db;
        _storage = storage;
        _parser = parser;
        _extraction = extraction;
        _logger = logger;
    }

    /// <summary>Загрузить документ: сохранение -> парсинг/OCR -> извлечение полей LLM.</summary>
    [HttpPost]
    [RequestSizeLimit(50_000_000)]
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DocumentDetailDto>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest("Пустой файл.");

        await using var stream = file.OpenReadStream();
        var stored = await _storage.SaveAsync(stream, file.FileName, ct);

        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = file.FileName,
            ContentType = file.ContentType,
            SizeBytes = stored.SizeBytes,
            StoragePath = stored.StoragePath,
            Status = DocumentStatus.Uploaded,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Documents.Add(document);
        await _db.SaveChangesAsync(ct);

        // Обработка синхронно (см. DECISIONS.md); Status готов к переводу в фон
        try
        {
            var parsed = await _parser.ExtractTextAsync(
                _storage.GetFullPath(document.StoragePath), document.FileName, document.ContentType, ct);

            document.ExtractedText = parsed.Text;
            document.UsedOcr = parsed.UsedOcr;
            document.Status = DocumentStatus.Parsed;
            await _db.SaveChangesAsync(ct);

            if (string.IsNullOrWhiteSpace(parsed.Text))
                throw new InvalidOperationException("Из документа не удалось извлечь текст.");

            var outcome = await _extraction.ExtractAsync(parsed.Text, ct);

            _db.ExtractionResults.Add(new ExtractionResult
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                Json = outcome.Json,
                Confidence = outcome.Confidence,
                Model = outcome.Model,
                PromptTokens = outcome.PromptTokens,
                CompletionTokens = outcome.CompletionTokens,
                CreatedAt = DateTimeOffset.UtcNow
            });
            document.Status = DocumentStatus.Extracted;
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, "Ошибка обработки документа {Id}", document.Id);
            document.Status = DocumentStatus.Failed;
            document.Error = e.Message;
            await _db.SaveChangesAsync(CancellationToken.None);
        }

        var dto = await BuildDetailDto(document.Id, ct);
        return CreatedAtAction(nameof(GetById), new { id = document.Id }, dto);
    }

    [HttpGet]
    public async Task<IReadOnlyList<DocumentListItemDto>> GetAll(CancellationToken ct) =>
        await _db.Documents
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DocumentListItemDto(
                d.Id, d.FileName, d.Status.ToString(), d.UsedOcr, d.SizeBytes, d.CreatedAt))
            .ToListAsync(ct);

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DocumentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var dto = await BuildDetailDto(id, ct);
        return dto is null ? NotFound() : dto;
    }

    private async Task<DocumentDetailDto?> BuildDetailDto(Guid id, CancellationToken ct) =>
        await _db.Documents
            .Include(d => d.Extraction)
            .Where(d => d.Id == id)
            .Select(d => new DocumentDetailDto(
                d.Id, d.FileName, d.Status.ToString(), d.UsedOcr, d.SizeBytes, d.CreatedAt, d.Error,
                d.Extraction == null
                    ? null
                    : new ExtractionDto(
                        d.Extraction.Json,
                        d.Extraction.Confidence,
                        d.Extraction.Model,
                        d.Extraction.PromptTokens,
                        d.Extraction.CompletionTokens)))
            .FirstOrDefaultAsync(ct);
}

public record DocumentListItemDto(
    Guid Id, string FileName, string Status, bool UsedOcr, long SizeBytes, DateTimeOffset CreatedAt);

public record DocumentDetailDto(
    Guid Id, string FileName, string Status, bool UsedOcr, long SizeBytes, DateTimeOffset CreatedAt,
    string? Error, ExtractionDto? Extraction);

public record ExtractionDto(
    string Json, double? Confidence, string Model, int PromptTokens, int CompletionTokens);
