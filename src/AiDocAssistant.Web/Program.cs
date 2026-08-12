using AiDocAssistant.Core.Abstractions;
using AiDocAssistant.Core.Services;
using AiDocAssistant.Infrastructure.Agent;
using AiDocAssistant.Infrastructure.Llm;
using AiDocAssistant.Infrastructure.Parsing;
using AiDocAssistant.Infrastructure.Persistence;
using AiDocAssistant.Infrastructure.Reports;
using AiDocAssistant.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Сервисы ---------------------------------------------------------------

builder.Services.AddControllers();

// PostgreSQL + pgvector через EF Core (UseVector регистрирует тип Vector)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        npgsql => npgsql.UseVector()));

// Конфигурация
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<DeepSeekOptions>(builder.Configuration.GetSection(DeepSeekOptions.SectionName));
builder.Services.Configure<EmbeddingOptions>(builder.Configuration.GetSection(EmbeddingOptions.SectionName));
builder.Services.Configure<RagOptions>(builder.Configuration.GetSection(RagOptions.SectionName));
builder.Services.AddSingleton(sp =>
{
    var options = new RagOptions();
    builder.Configuration.GetSection(RagOptions.SectionName).Bind(options);
    return options;
});

// Файлы и парсинг
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddSingleton<OcrCli>();
builder.Services.AddScoped<IDocumentParser, PdfDocumentParser>();
builder.Services.AddScoped<IDocumentParser, ImageDocumentParser>();
builder.Services.AddScoped<CompositeDocumentParser>();

// LLM: DeepSeek за интерфейсом ILlmProvider (model-agnostic) + телеметрия Фазы 4
builder.Services.Configure<LlmPricingOptions>(builder.Configuration.GetSection(LlmPricingOptions.SectionName));
builder.Services.AddSingleton<LlmCostEstimator>(sp =>
    new LlmCostEstimator(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LlmPricingOptions>>().Value));
builder.Services.AddHttpClient<DeepSeekLlmProvider>();
builder.Services.AddScoped<ILlmUsageStore, EfLlmUsageStore>();
builder.Services.AddScoped<ILlmProvider>(sp => new MeteringLlmProvider(
    sp.GetRequiredService<DeepSeekLlmProvider>(),
    sp.GetRequiredService<ILlmUsageStore>(),
    sp.GetRequiredService<LlmCostEstimator>()));
builder.Services.AddScoped<DocumentExtractionService>();

// RAG-индексация: чанкер + эмбеддер + хранилище на pgvector
builder.Services.AddSingleton<ITextChunker, RecursiveTextChunker>();
builder.Services.AddHttpClient<IEmbeddingProvider, OpenAiCompatibleEmbeddingProvider>();
builder.Services.AddScoped<IChunkStore, PgVectorChunkStore>();
builder.Services.AddScoped<DocumentIndexingService>();

// RAG-чат: сессии + ответ с цитатами
builder.Services.AddScoped<IChatSessionStore, EfChatSessionStore>();
builder.Services.AddScoped<RagChatService>();

// Фаза 3: agent tools
builder.Services.AddSingleton<DocumentReconcileService>();
builder.Services.AddSingleton<DocumentReportService>();
builder.Services.AddSingleton<DocumentReportXlsxWriter>();
builder.Services.AddScoped<DocumentSummarizeService>();
builder.Services.AddScoped<IAgentTaskStore, EfAgentTaskStore>();
builder.Services.AddScoped<IAgentTool, ReconcileAgentTool>();
builder.Services.AddScoped<IAgentTool, SummarizeAgentTool>();
builder.Services.AddScoped<IAgentTool, GenerateReportAgentTool>();
builder.Services.AddScoped<AgentToolRegistry>();
builder.Services.AddScoped<AgentGoalRouterService>();
builder.Services.AddScoped<AgentGoalService>();
builder.Services.AddScoped<AgentTaskService>();

// Фаза 4: метрики и evals
builder.Services.AddScoped<IDataCountsProvider, EfDataCountsProvider>();
builder.Services.AddSingleton<EvalSuiteService>();
builder.Services.AddScoped<MetricsService>();

// Health-check: проверяет и сам сервис, и доступность БД
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// Swagger — оставляем включённым во всех окружениях (портфолио-проект)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Миграции при старте ----------------------------------------------------
// Осознанный trade-off (см. DECISIONS.md): для демо-проекта с одной репликой
// автомиграция упрощает запуск до "docker-compose up". В проде с несколькими
// инстансами так делать нельзя — миграции выносят в отдельный шаг деплоя.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// --- Pipeline ----------------------------------------------------------------

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Нужно для WebApplicationFactory в интеграционных тестах
public partial class Program { }
