using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AiDocAssistant.Infrastructure.Parsing;

/// <summary>
/// OCR через CLI-процессы: tesseract (распознавание) и pdftoppm (растеризация PDF).
/// Выбор CLI вместо .NET-обёрток — см. DECISIONS.md (нет нативных зависимостей,
/// одинаково работает на Windows и в Linux-контейнере).
/// </summary>
public class OcrCli
{
    private readonly ILogger<OcrCli> _logger;

    public OcrCli(ILogger<OcrCli> logger) => _logger = logger;

    /// <summary>Распознать текст с изображения. Языки: русский + английский.</summary>
    public async Task<string> RecognizeImageAsync(string imagePath, CancellationToken ct = default)
    {
        // "stdout" вместо имени выходного файла -> текст сразу в поток
        var output = await RunAsync("tesseract", $"\"{imagePath}\" stdout -l rus+eng", ct);
        return output.Trim();
    }

    /// <summary>Скан-PDF: растеризуем страницы в PNG и прогоняем через OCR.</summary>
    public async Task<string> RecognizePdfAsync(string pdfPath, CancellationToken ct = default)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var prefix = Path.Combine(tempDir, "page");
            // -r 200: 200 DPI — баланс качества распознавания и скорости
            await RunAsync("pdftoppm", $"-png -r 200 \"{pdfPath}\" \"{prefix}\"", ct);

            var sb = new StringBuilder();
            foreach (var page in Directory.GetFiles(tempDir, "page*.png").OrderBy(f => f))
            {
                sb.AppendLine(await RecognizeImageAsync(page, ct));
                sb.AppendLine();
            }
            return sb.ToString().Trim();
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (Exception e) { _logger.LogWarning(e, "Не удалось удалить временную папку OCR {Dir}", tempDir); }
        }
    }

    private static async Task<string> RunAsync(string command, string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Не удалось запустить процесс {command}");

        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"{command} завершился с кодом {process.ExitCode}: {stderr}. " +
                $"Убедись, что {command} установлен и доступен в PATH.");

        return stdout;
    }
}
