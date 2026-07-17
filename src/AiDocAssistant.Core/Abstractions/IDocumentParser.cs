namespace AiDocAssistant.Core.Abstractions;

/// <summary>
/// Извлечение текста из файла документа. Реализации по типам файлов
/// регистрируются в DI, роутинг делает CompositeDocumentParser.
/// </summary>
public interface IDocumentParser
{
    bool Supports(string fileName, string contentType);
    Task<ParsedDocument> ExtractTextAsync(string filePath, CancellationToken ct = default);
}

public sealed record ParsedDocument(string Text, bool UsedOcr);
