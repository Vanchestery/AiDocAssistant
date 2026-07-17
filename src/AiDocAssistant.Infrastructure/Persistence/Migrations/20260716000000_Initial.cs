using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiDocAssistant.Infrastructure.Persistence.Migrations;

/// <summary>
/// Начальная миграция: включает расширение pgvector в базе.
/// Таблиц пока нет — они появятся в Фазе 1.
/// </summary>
public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:vector", ",,");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
    }
}
