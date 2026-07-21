using AiDocAssistant.Core.Abstractions;

namespace AiDocAssistant.Core.Services;

/// <summary>
/// Рекурсивный чанкер по иерархии разделителей (абзац → строка → предложение → слово → символ).
/// Режет по самой крупной естественной границе, которая помещается в MaxChars,
/// и склеивает мелкие куски в чанки нужного размера с перекрытием (overlap).
/// Тот же принцип, что у RecursiveCharacterTextSplitter в LangChain — индустриальный дефолт RAG.
/// См. DECISIONS.md №12.
/// </summary>
public sealed class RecursiveTextChunker : ITextChunker
{
    // От крупного к мелкому. Пустая строка в конце = резать по символам (крайний случай).
    private static readonly string[] Separators = ["\n\n", "\n", ". ", " ", ""];

    public IReadOnlyList<TextChunk> Chunk(string text, ChunkingOptions? options = null)
    {
        options ??= ChunkingOptions.Default;
        if (options.Overlap >= options.MaxChars)
            throw new ArgumentException("Overlap должен быть меньше MaxChars.", nameof(options));

        if (string.IsNullOrWhiteSpace(text))
            return [];

        var pieces = SplitRecursive(text.Trim(), Separators, options);

        var chunks = new List<TextChunk>(pieces.Count);
        var index = 0;
        foreach (var piece in pieces)
        {
            var trimmed = piece.Trim();
            if (trimmed.Length > 0)
                chunks.Add(new TextChunk(index++, trimmed));
        }
        return chunks;
    }

    /// <summary>
    /// Выбирает крупнейший разделитель, присутствующий в тексте, режет по нему.
    /// Куски меньше MaxChars копятся и склеиваются; куски больше — рекурсивно
    /// дробятся следующим (более мелким) разделителем.
    /// </summary>
    private static List<string> SplitRecursive(string text, string[] separators, ChunkingOptions options)
    {
        var result = new List<string>();

        var separator = separators[^1];
        var remaining = Array.Empty<string>();
        for (var i = 0; i < separators.Length; i++)
        {
            var s = separators[i];
            if (s.Length == 0) { separator = s; break; }
            if (text.Contains(s, StringComparison.Ordinal))
            {
                separator = s;
                remaining = separators[(i + 1)..];
                break;
            }
        }

        var splits = separator.Length == 0
            ? text.Select(c => c.ToString()).ToArray()
            : text.Split(separator).Where(s => s.Length > 0).ToArray();

        var good = new List<string>();
        foreach (var part in splits)
        {
            if (part.Length < options.MaxChars)
            {
                good.Add(part);
                continue;
            }

            // Накопленные мелкие куски склеиваем, затем крупный кусок дробим глубже.
            if (good.Count > 0)
            {
                result.AddRange(MergeWithOverlap(good, separator, options));
                good.Clear();
            }

            if (remaining.Length == 0)
                result.Add(part);
            else
                result.AddRange(SplitRecursive(part, remaining, options));
        }

        if (good.Count > 0)
            result.AddRange(MergeWithOverlap(good, separator, options));

        return result;
    }

    /// <summary>
    /// Жадно упаковывает куски (снова через их разделитель) в чанки до MaxChars.
    /// При переполнении сдвигает окно, оставляя «хвост» длиной ~Overlap —
    /// так соседние чанки перекрываются и контекст не рвётся на границе.
    /// </summary>
    private static IEnumerable<string> MergeWithOverlap(IReadOnlyList<string> splits, string separator, ChunkingOptions options)
    {
        var sepLen = separator.Length;
        var docs = new List<string>();
        var window = new List<string>();
        var total = 0;

        foreach (var part in splits)
        {
            var addSep = window.Count > 0 ? sepLen : 0;

            if (total + part.Length + addSep > options.MaxChars && window.Count > 0)
            {
                var doc = string.Join(separator, window).Trim();
                if (doc.Length > 0)
                    docs.Add(doc);

                // Сдвигаем левую границу окна, пока не уложимся в лимит и не срежем перекрытие.
                while (total > options.Overlap ||
                       (total + part.Length + (window.Count > 0 ? sepLen : 0) > options.MaxChars && total > 0))
                {
                    total -= window[0].Length + (window.Count > 1 ? sepLen : 0);
                    window.RemoveAt(0);
                    if (window.Count == 0) break;
                }
            }

            window.Add(part);
            total += part.Length + (window.Count > 1 ? sepLen : 0);
        }

        var last = string.Join(separator, window).Trim();
        if (last.Length > 0)
            docs.Add(last);

        return docs;
    }
}
