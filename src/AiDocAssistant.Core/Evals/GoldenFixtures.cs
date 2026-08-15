using System.Reflection;

namespace AiDocAssistant.Core.Evals;

/// <summary>Загрузка golden JSON из embedded resources (Фаза 4, шаг 3).</summary>
public static class GoldenFixtures
{
    public static string Load(string fileName)
    {
        var assembly = typeof(GoldenFixtures).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith($".{fileName}", StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Ресурс не найден: {fileName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
