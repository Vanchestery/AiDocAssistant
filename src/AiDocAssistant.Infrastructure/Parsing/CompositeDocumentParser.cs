using AiDocAssistant.Core.Abstractions;

namespace AiDocAssistant.Infrastructure.Parsing;

/// <summary>Выбирает подходящий парсер по типу файла.</summary>
public class CompositeDocumentParser
{
    private readonly IEnumerable<IDocumentParser> _parsers;

    public CompositeDocumentParser(IEnumerable<IDocumentParser> parsers) => _parsers = parsers;

    public Task<ParsedDocument> ExtractTextAsync(
        string filePath, string fileName, string contentType, CancellationToken ct = default)
    {
        var parser = _parsers.FirstOrDefault(p => p.Supports(fileName, contentType))
            ?? throw new NotSupportedException(
                $"Формат файла не поддерживается: {fileName}. Поддерживаются PDF и изображения (png/jpg/tiff/bmp).");

        return parser.ExtractTextAsync(filePath, ct);
    }
}
