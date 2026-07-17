using AiDocAssistant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Сервисы ---------------------------------------------------------------

builder.Services.AddControllers();

// PostgreSQL + pgvector через EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

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
