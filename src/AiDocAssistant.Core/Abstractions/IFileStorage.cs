namespace AiDocAssistant.Core.Abstractions;

/// <summary>
/// Хранилище исходных файлов. Сейчас — локальный диск;
/// интерфейс позволяет добавить S3/MinIO без изменения бизнес-логики.
/// </summary>
public interface IFileStorage
{
    Task<StoredFile> SaveAsync(Stream content, string originalFileName, CancellationToken ct = default);

    /// <summary>Абсолютный путь к файлу по его StoragePath.</summary>
    string GetFullPath(string storagePath);
}

public sealed record StoredFile(string StoragePath, long SizeBytes);
