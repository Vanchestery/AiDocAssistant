namespace AiDocAssistant.Infrastructure.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Корневая папка хранилища. Относительный путь — от рабочей директории приложения.</summary>
    public string Root { get; set; } = "uploads";
}
