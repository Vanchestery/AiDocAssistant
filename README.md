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
- **UI (Blazor):** http://localhost:8080/
- Health-check: http://localhost:8080/health

Для разработки в Visual Studio: поднять только БД (`docker-compose up db`) и запустить проект `AiDocAssistant.Web` (профиль http, порт 5080). Миграции применяются автоматически при старте.

### Конфигурация

API-ключ DeepSeek (в git не попадает):

- локально — user-secrets: `dotnet user-secrets set "DeepSeek:ApiKey" "sk-..." --project src/AiDocAssistant.Web`
- в Docker — файл `.env` рядом с docker-compose.yml: `DEEPSEEK_API_KEY=sk-...`

Для OCR локально нужны [Tesseract](https://github.com/UB-Mannheim/tesseract/wiki) (с русским языком) и [poppler](https://github.com/oschwartz10612/poppler-windows/releases) в PATH; в Docker-образе они уже установлены.

В Docker Ollama должна быть запущена **на хосте**; в `docker-compose.yml` для API задано `Embedding__BaseUrl=http://host.docker.internal:11434`.

### Использование

`POST /api/documents` (multipart, поле `file`) — загрузка PDF или изображения: текст извлекается (при необходимости OCR), LLM возвращает структурированные поля (номер, дата, контрагент, позиции, суммы) с confidence. `GET /api/documents` — список, `GET /api/documents/{id}` — детали с JSON извлечения.

**RAG-чат:** `POST /api/chat/sessions` — новая сессия; `POST /api/chat/sessions/{id}/messages` — вопрос (JSON: `{ "question": "...", "documentId": null }`); в ответе — текст ассистента и массив **citations** (какие документы/фрагменты использованы). `GET /api/chat/sessions/{id}` — история диалога. **UI:** `/chat` и `/chat/{sessionId}` — Blazor-страница с цитатами и фильтром по документу. Перед чатом документ должен быть загружен и проиндексирован (нужна Ollama с `bge-m3`).

**Агент (Фаза 3):** `GET /api/agent/tools` — список tools; `POST /api/agent/tasks` — явный tool (`reconcile` / `summarize` / `generate_report` + `documentIds`); `POST /api/agent/goals` — `{ "goal": "сверь счета...", "documentIds": [...] }` (LLM выбирает tool); `GET /api/agent/tasks/{id}` — результат; `GET /api/agent/tasks/{id}/report` — xlsx после `generate_report`. **UI:** `/agent` — выбор документов, явный tool или goal-mode, скачивание отчёта.

**Метрики (Фаза 4):** `GET /api/metrics/summary` — LLM-токены, latency, оценка стоимости, счётчики БД, eval-кейсы; `GET /api/metrics/evals` — только evals. **UI:** `/metrics` — dashboard с eval-suite.

Всё видно в Swagger и в Blazor UI (`/`, `/documents`, `/chat`, `/agent`, `/metrics`).

## Деплой (Docker)

**Разработка / демо:**
```bash
docker compose up --build -d
```

**Production-override** (переменные окружения `Production`, автоперезапуск контейнеров):
```bash
cp .env.example .env   # задай DEEPSEEK_API_KEY
docker compose -f docker-compose.yml -f docker-compose.prod.yml up --build -d
```

- UI: http://localhost:8080/
- Swagger: http://localhost:8080/swagger
- Health: http://localhost:8080/health

Данные Postgres и uploads сохраняются в Docker volumes (`pgdata`, `uploads`). `docker compose down` volumes **не удаляет**; `down -v` — удалит.

## Структура

```
src/
  AiDocAssistant.Core/            — домен: сущности, интерфейсы, бизнес-логика
  AiDocAssistant.Infrastructure/  — EF Core, pgvector, LLM-клиенты, парсеры
  AiDocAssistant.Web/             — Web API, Blazor UI, DI, Swagger
tests/
  AiDocAssistant.Tests/           — xUnit
```

Архитектурные решения и их обоснование — в [DECISIONS.md](DECISIONS.md).

## Статус (фазы)

- [x] Фаза 0 — каркас: solution, Docker (Postgres+pgvector), миграции, health-check, Swagger
- [x] Фаза 1 — загрузка документов (PDF/сканы, OCR) и structured extraction через LLM
- [x] Фаза 2 — RAG: эмбеддинги, векторный поиск, чат с цитатами
- [x] Фаза 3 — ИИ-агент с tool-use (сверка, сводка, отчёт, goal-mode)
- [x] Фаза 4 — evals, метрики точности/стоимости/latency
- [x] Фаза 5 — frontend (Blazor) и деплой
- [ ] Фаза 6 — MCP-сервер

## Метрики

`GET /api/metrics/summary` — агрегаты LLM-вызовов (токены, latency, оценка USD), счётчики документов/чатов/agent tasks, eval-кейсы. `GET /api/metrics/evals` — детерминированные evals (reconcile + поля extraction). Тарифы токенов — `LlmPricing` в appsettings.
