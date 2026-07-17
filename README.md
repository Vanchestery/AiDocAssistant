# AiDocAssistant

AI-ассистент для автоматизации документооборота бэк-офиса: загрузка документов (PDF/сканы), извлечение структурированных данных, вопросы по содержимому (RAG с цитатами) и агентные действия — сверка, сводки, отчёты.

## Стек

ASP.NET Core (.NET 8) · PostgreSQL + pgvector · EF Core · LLM через OpenAI-совместимый API (model-agnostic, провайдер за интерфейсом `ILlmProvider`) · Docker · xUnit.

## Быстрый старт

Требуется Docker Desktop.

```bash
docker-compose up --build
```

- API + Swagger: http://localhost:8080/swagger
- Health-check: http://localhost:8080/health

Для разработки в Visual Studio: поднять только БД (`docker-compose up db`) и запустить проект `AiDocAssistant.Web` (профиль http, порт 5080). Миграции применяются автоматически при старте.

## Структура

```
src/
  AiDocAssistant.Core/            — домен: сущности, интерфейсы, бизнес-логика
  AiDocAssistant.Infrastructure/  — EF Core, pgvector, LLM-клиенты, парсеры
  AiDocAssistant.Web/             — Web API, DI, Swagger
tests/
  AiDocAssistant.Tests/           — xUnit
```

Архитектурные решения и их обоснование — в [DECISIONS.md](DECISIONS.md).

## Статус (фазы)

- [x] Фаза 0 — каркас: solution, Docker (Postgres+pgvector), миграции, health-check, Swagger
- [ ] Фаза 1 — загрузка документов и structured extraction
- [ ] Фаза 2 — RAG: эмбеддинги, векторный поиск, чат с цитатами
- [ ] Фаза 3 — ИИ-агент с tool-use (сверка, сводка, отчёт)
- [ ] Фаза 4 — evals, метрики точности/стоимости/latency
- [ ] Фаза 5 — frontend (Blazor) и деплой
- [ ] Фаза 6 — MCP-сервер

## Метрики

Появятся в Фазе 4: точность извлечения/ответов, стоимость запроса, latency, покрытие тестами.
