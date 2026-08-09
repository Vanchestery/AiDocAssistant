using AiDocAssistant.Core.Abstractions;

namespace AiDocAssistant.Core.Services;

/// <summary>
/// Индексация документа под RAG: текст -> чанки -> эмбеддинги -> хранилище.
/// Оркестрация в Core; конкретные чанкер/эмбеддер/хранилище — за интерфейсами.
/// </summary>
public sealed class DocumentIndexingService
{
    private readonly ITextChunker _chunker;
    private readonly IEmbeddingProvider _embeddings;
    private readonly IChunkStore _store;

    public DocumentIndexingService(ITextChunker chunker, IEmbeddingProvider embeddings, IChunkStore store)
    {
        _chunker = chunker;
        _embeddings = embeddings;
        _store = store;
    }

    /// <summary>Возвращает число проиндексированных чанков.</summary>
    public async Task<int> IndexAsync(Guid documentId, string text, CancellationToken ct = default)
    {
        var chunks = _chunker.Chunk(text);
        if (chunks.Count == 0)
            return 0;

        var vectors = await _embeddings.EmbedAsync(chunks.Select(c => c.Text).ToArray(), ct);
        if (vectors.Count != chunks.Count)
            throw new InvalidOperationException(
                $"Эмбеддер вернул {vectors.Count} векторов на {chunks.Count} чанков.");

        var records = chunks
            .Zip(vectors, (c, v) => new ChunkRecord(c.Index, c.Text, v))
            .ToArray();

        await _store.ReplaceForDocumentAsync(documentId, records, ct);
        return records.Length;
    }
}
