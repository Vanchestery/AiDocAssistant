# --- Стадия сборки -----------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Сначала копируем только csproj-файлы — Docker закэширует слой с restore,
# и пересборка при изменении кода не будет заново качать пакеты
COPY src/AiDocAssistant.Core/AiDocAssistant.Core.csproj src/AiDocAssistant.Core/
COPY src/AiDocAssistant.Infrastructure/AiDocAssistant.Infrastructure.csproj src/AiDocAssistant.Infrastructure/
COPY src/AiDocAssistant.Web/AiDocAssistant.Web.csproj src/AiDocAssistant.Web/
RUN dotnet restore src/AiDocAssistant.Web/AiDocAssistant.Web.csproj

COPY src/ src/
RUN dotnet publish src/AiDocAssistant.Web/AiDocAssistant.Web.csproj -c Release -o /app/publish

# --- Стадия запуска ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# OCR-инструменты: tesseract (распознавание, rus+eng) и poppler (растеризация скан-PDF)
RUN apt-get update && apt-get install -y --no-install-recommends \
        tesseract-ocr tesseract-ocr-rus poppler-utils \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AiDocAssistant.Web.dll"]
