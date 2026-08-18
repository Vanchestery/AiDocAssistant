# Скриншоты для GitHub README

Положи готовые PNG в **эту папку** (`docs/screenshots/`). README ссылается на файлы ниже — имена **строго** как указано.

---

## Минимум (4 шота — хватит для портфолио)

| Файл | Что снять | URL / где |
|------|-----------|-----------|
| **`01-documents.png`** | Список документов: несколько строк, статусы **Extracted**, видны schet A/B | http://localhost:8080/documents |
| **`03-agent-reconcile.png`** | Agent: выбраны 2 счёта, режим reconcile **или** goal, виден результат (расхождения 112700 vs 115000) | http://localhost:8080/agent |
| **`04-metrics.png`** | Dashboard: LLM stats + таблица eval-кейсов (14 cases) | http://localhost:8080/metrics |
| **`05-mcp-cursor.png`** | Cursor: MCP **Connected** + фрагмент чата с `list_documents` или `reconcile` | Cursor Settings → MCP + чат |

## Рекомендуется (+2)

| Файл | Что снять |
|------|-----------|
| **`02-extraction.png`** | Детали документа: JSON extraction (номер, сумма, контрагент) |
| **`06-rag-chat.png`** | Чат: вопрос + ответ + блок **citations** (нужна Ollama + проиндексированный doc) |

## Опционально

| Файл | Что снять |
|------|-----------|
| **`07-swagger.png`** | Swagger UI с раскрытым `/api/documents` или `/api/agent` |
| **`00-banner.png`** | Широкий кроп главной `/` или коллаж — для social preview (см. ниже) |

---

## Подготовка перед съёмкой

```powershell
cd C:\Users\ivan-\source\repos\AiDocAssistant
docker compose up -d
# Проверь: http://localhost:8080/health → Healthy
```

1. **Данные:** в БД уже есть schet A/B — не чисти volume.
2. **Секреты:** на скринах **не должно** быть API keys, `.env`, содержимого `.cursor/mcp.json`.
3. **Окно:** браузер **1280×720** или ширина ~1400 px (DevTools F12 → Toggle device toolbar → Responsive 1280).
4. **Тема:** светлая или тёмная — **одна** на всех UI-скринах (Blazor по умолчанию светлая — ок).
5. **Язык UI:** русский интерфейс Blazor — норм для портфолио в RU; для международного README можно подписи на EN в caption (уже в README).

### Agent reconcile (шаги)

1. `/agent` → отметить **schet A** и **schet B** (Extracted).
2. Режим **reconcile** → Run → дождаться Completed.
3. Скрин: чекбоксы + JSON/текст с **2 расхождениями** (total, vat).

### MCP (шаги)

1. MCP toggle **ON**, Local **Connected**.
2. В чате виден вызов tool и таблица/JSON (как у тебя уже было).
3. **Обрежь** окно: Settings MCP + кусок чата в одном кадре **или** два окна рядом (Win+← / Win+→).

### RAG (если снимаешь 06)

1. Ollama: `ollama pull bge-m3`, ollama running.
2. Документ Extracted → переиндексировать (re-upload или уже indexed).
3. `/chat` → вопрос «какая итоговая сумма в счёте?» → видны **citations**.

---

## Как снять (Windows)

| Способ | Как |
|--------|-----|
| **Win + Shift + S** | Область → сохранить → переименовать в `01-documents.png` |
| **Snipping Tool** | Win, набери «Ножницы» |
| **Chrome full page** | F12 → Ctrl+Shift+P → `Capture full size screenshot` (длинные страницы) |

Сохраняй как **PNG** (не JPEG — текст ч sharper).

---

## После съёмки

```powershell
# Файлы должны лежать здесь:
dir docs\screenshots\*.png

git add docs/screenshots/
git commit -m "docs: add README screenshots"
git push
```

README подхватит картинки автоматически.

---

## Social preview (GitHub)

**Settings → General → Social preview** — загрузи **`00-banner.png`** (1280×640).

Сгенерирован автоматически (`scripts/pack-screenshots.ps1`): текст + MCP reconcile + мини metrics/RAG.

Пересобрать после новых скринов:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/pack-screenshots.ps1
```

---

## Чеклист качества

- [ ] Текст читаемый (не слишком мелкий crop)
- [ ] Нет ключей / паролей / личной почты
- [ ] Статус **Extracted**, не Failed
- [ ] Agent показывает **осмысленный** результат (сверка)
- [ ] Metrics: evals видны
- [ ] MCP: **Connected** зелёный
