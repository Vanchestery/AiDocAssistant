namespace AiDocAssistant.Infrastructure.Llm;

public class DeepSeekOptions
{
    public const string SectionName = "DeepSeek";

    /// <summary>Ключ задаётся через user-secrets или переменную окружения, НЕ в appsettings.json.</summary>
    public string ApiKey { get; set; } = "";

    public string BaseUrl { get; set; } = "https://api.deepseek.com";
    public string Model { get; set; } = "deepseek-chat";
}
