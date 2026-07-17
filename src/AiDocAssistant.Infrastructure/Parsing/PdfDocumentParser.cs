using System.Text;
using AiDocAssistant.Core.Abstractions;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace AiDocAssistant.Infrastructure.Parsing;

/// <summary>
/// PDF: сначала пытаемся взять текстовый слой (PdfPig).
/// Если его нет (скан) — fallback на OCR.
/// </summary>
public class PdfDocumentParser : IDocumentParser
{
    // Меньше этого количества символов считаем, что текстового слоя нет
    private const int MinTextLength = 50;

    private readonly OcrCli _ocr;
    private readonly ILogger<PdfDocumentParser> _logger;

    public PdfDocumentParser(OcrCli ocr, ILogger<PdfDocumentParser> logger)
    {
        _ocr = ocr;
        _logger = logger;
    }

    public bool Supports(string fileName, string contentType) =>
        Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
        || contentType == "application/pdf";

    public async Task<ParsedDocument> ExtractTextAsync(string filePath, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        using (var pdf = PdfDocument.Open(filePath))
        {
            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
                sb.AppendLine();
            }
        }

        var text = sb.ToString().Trim();
        if (text.Length >= MinTextLength)
            return new ParsedDocument(text, UsedOcr: false);

        _logger.LogInformation("PDF {Path} без текстового слоя — переключаюсь на OCR", filePath);
        var ocrText = await _ocr.RecognizePdfAsync(filePath, ct);
        return new ParsedDocument(ocrText, UsedOcr: true);
    }
}
