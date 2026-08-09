namespace AiDocAssistant.Core.Abstractions;

/// <summary>
/// Хранилище чанков с векторным поиском. Реализация — pgvector поверх EF Core.
/// Интерфейс работает с float[] и не тянет тип Vector в Core-логику.
/// </summary>
public interface IChunkStore
{
    /// <summary>
    /// Заменить все чанки документа новыми (идемпотентная переиндексация):
    /// старые удаляются, новые сохраняются. Так повторная загрузка не плодит дубли.
    /// </summary>
    Task ReplaceForDocumentAsync(Guid documentId, IReadOnlyList<ChunkRecord> chunks, CancellationToken ct = default);

    /// <summary>
    /// Top-K ближайших к запросу чанков по косинусной близости.
    /// documentId — опциональный фильтр «только этот документ».
    /// </summary>
    Task<IReadOnlyList<ChunkHit>> SearchAsync(
        float[] queryEmbedding,
        int topK,
        Guid? documentId = null,
        CancellationToken ct = default);
}

/// <summary>Готовый к сохранению чанк: текст + его эмбеддинг.</summary>
public sealed record ChunkRecord(int Ordinal, string Text, float[] Embedding);

/// <summary>Результат поиска: чанк + имя документа + дистанция (меньше = ближе).</summary>
public sealed record ChunkHit(
    Guid DocumentId,
    string DocumentFileName,
    int Ordinal,
    string Text,
    double Distance);
