# Инструкция по настройке окружения

## Установка EF Core Tools

Для работы с миграциями необходимо установить глобальный инструмент EF Core:

```bash
dotnet tool install --global dotnet-ef
```

Если инструмент уже установлен, обновите его до последней версии:

```bash
dotnet tool update --global dotnet-ef
```

Проверьте установку:

```bash
dotnet ef --version
```

## Первоначальная настройка

### Шаг 1: Запуск PostgreSQL

```bash
docker-compose up -d
```

Дождитесь, пока контейнер полностью запустится (проверьте статус):

```bash
docker-compose ps
```

### Шаг 2: Создание первой миграции

```bash
dotnet ef migrations add InitialCreate --project ArchiveWeb
```

Это создаст папку `Migrations` в проекте `ArchiveWeb` с файлами миграции.

### Шаг 3: Применение миграций к базе данных

```bash
dotnet ef database update --project ArchiveWeb
```

После выполнения этой команды база данных будет создана со всеми необходимыми таблицами.

### Шаг 4: Проверка подключения

Запустите приложение:

```bash
dotnet run --project ArchiveWeb
```

Если все настроено правильно, приложение запустится без ошибок подключения к БД.

## Устранение проблем

### Ошибка подключения к БД

1. Убедитесь, что контейнер PostgreSQL запущен:
   ```bash
   docker-compose ps
   ```

2. Проверьте логи контейнера:
   ```bash
   docker-compose logs postgres
   ```

3. Проверьте строку подключения в `appsettings.json`:
   - Host: localhost
   - Port: 5435
   - Database: sachkov_tech
   - Username: postgres
   - Password: postgres

### Ошибка "dotnet ef: command not found"

Установите EF Core Tools (см. раздел выше).

### Ошибка при создании миграции

Убедитесь, что:
1. Все NuGet пакеты восстановлены: `dotnet restore`
2. Проект компилируется: `dotnet build`
3. DbContext правильно настроен в `Program.cs`

