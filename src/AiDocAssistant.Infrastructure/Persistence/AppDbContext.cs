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
    public DbSet<Chunk> Chunks => Set<Chunk>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<AgentAction> AgentActions => Set<AgentAction>();
    public DbSet<LlmUsageEvent> LlmUsageEvents => Set<LlmUsageEvent>();

    // Размерность вектора модели эмбеддингов (bge-m3 = 1024). Меняешь модель с
    // другой размерностью — нужна новая миграция под vector(N). См. DECISIONS.md №14.
    public const int EmbeddingDimensions = 1024;

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

        modelBuilder.Entity<Chunk>(b =>
        {
            b.Property(x => x.Text).HasColumnType("text");
            b.Property(x => x.Embedding).HasColumnType($"vector({EmbeddingDimensions})");

            // HNSW-индекс с косинусным оператором — быстрый ANN-поиск ближайших векторов.
            b.HasIndex(x => x.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");

            b.HasIndex(x => x.DocumentId);

            b.HasOne<Document>()
                .WithMany()
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatSession>(b =>
        {
            b.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<ChatMessage>(b =>
        {
            b.Property(x => x.Content).HasColumnType("text");
            b.Property(x => x.CitationsJson).HasColumnType("jsonb");
            b.Property(x => x.Model).HasMaxLength(200);
            b.HasIndex(x => x.SessionId);
            b.HasIndex(x => x.CreatedAt);

            b.HasOne(x => x.Session)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AgentAction>(b =>
        {
            b.Property(x => x.Tool).HasMaxLength(100);
            b.Property(x => x.InputJson).HasColumnType("jsonb");
            b.Property(x => x.ResultJson).HasColumnType("jsonb");
            b.HasIndex(x => x.CreatedAt);
            b.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<LlmUsageEvent>(b =>
        {
            b.Property(x => x.Operation).HasMaxLength(100);
            b.Property(x => x.Model).HasMaxLength(200);
            b.Property(x => x.EstimatedCostUsd).HasPrecision(18, 8);
            b.HasIndex(x => x.CreatedAt);
            b.HasIndex(x => x.Operation);
        });

        base.OnModelCreating(modelBuilder);
    }
}
