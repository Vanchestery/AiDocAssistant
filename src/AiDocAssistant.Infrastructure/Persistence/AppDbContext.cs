using AiDocAssistant.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiDocAssistant.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<ExtractionResult> ExtractionResults => Set<ExtractionResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Включаем расширение pgvector на уровне модели:
        // EF сгенерирует CREATE EXTENSION в миграции, и схема БД
        // полностью воспроизводится одной командой на любой машине.
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Document>(b =>
        {
            b.Property(x => x.FileName).HasMaxLength(500);
            b.Property(x => x.ContentType).HasMaxLength(200);
            b.Property(x => x.StoragePath).HasMaxLength(1000);
            b.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<ExtractionResult>(b =>
        {
            b.Property(x => x.Json).HasColumnType("jsonb");
            b.Property(x => x.Model).HasMaxLength(200);
            b.HasOne(x => x.Document)
                .WithOne(x => x.Extraction)
                .HasForeignKey<ExtractionResult>(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
