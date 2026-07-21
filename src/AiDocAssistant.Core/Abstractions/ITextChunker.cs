namespace AiDocAssistant.Core.Abstractions;

/// <summary>
/// Разбивка текста документа на фрагменты (чанки) под эмбеддинги и векторный поиск.
/// Стратегия спрятана за интерфейсом — можно подменить/сравнить в evals (Фаза 4).
/// </summary>
public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(string text, ChunkingOptions? options = null);
}

/// <summary>Один фрагмент текста. Index — порядковый номер внутри документа (для цитат).</summary>
public sealed record TextChunk(int Index, string Text)
{
    public int CharCount => Text.Length;
}

/// <summary>
/// Параметры чанкинга. Размер и перекрытие — в символах, а не токенах:
/// без внешнего токенайзера, model-agnostic (см. DECISIONS.md №12).
/// </summary>
public sealed record ChunkingOptions(int MaxChars = 1000, int Overlap = 150)
{
    public static ChunkingOptions Default { get; } = new();
}
