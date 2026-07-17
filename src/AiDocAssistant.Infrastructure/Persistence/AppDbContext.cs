using Microsoft.EntityFrameworkCore;

namespace AiDocAssistant.Infrastructure.Persistence;

/// <summary>
/// Главный контекст EF Core. Сущности (Document, Chunk и т.д.) добавим в Фазе 1.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Включаем расширение pgvector на уровне модели:
        // EF сгенерирует CREATE EXTENSION в миграции, и схема БД
        // полностью воспроизводится одной командой на любой машине.
        modelBuilder.HasPostgresExtension("vector");

        base.OnModelCreating(modelBuilder);
    }
}
