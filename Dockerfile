FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости
COPY *.csproj .
RUN dotnet restore

# Копируем весь код
COPY . .

# Собираем приложение
RUN dotnet publish -c Release -o /app/publish

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Устанавливаем SQLite
RUN apt-get update && apt-get install -y \
    sqlite3 \
    && rm -rf /var/lib/apt/lists/*

# Создаем директории для данных
RUN mkdir -p /app/data /app/wwwroot/images

# Копируем опубликованные файлы
COPY --from=build /app/publish .

# Копируем статические файлы (если есть)
COPY wwwroot /app/wwwroot

# Устанавливаем права
RUN chmod -R 755 /app/data

EXPOSE 8080

# Запускаем приложение
ENTRYPOINT ["dotnet", "WebAtena.dll"]