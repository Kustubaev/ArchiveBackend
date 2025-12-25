# Шаги для создания и применения миграций

## Важно!

Перед созданием миграций убедитесь, что:
1. ✅ Приложение **НЕ запущено** (остановите `dotnet run` если оно работает)
2. ✅ PostgreSQL контейнер **запущен** (`docker-compose up -d`)
3. ✅ Установлен EF Core Tools (`dotnet tool install --global dotnet-ef`)

## Шаги выполнения

### 1. Запустите PostgreSQL (если еще не запущен)

```bash
docker-compose up -d
```

Проверьте статус:

```bash
docker-compose ps
```

### 2. Убедитесь, что приложение остановлено

Если приложение запущено, остановите его (Ctrl+C в терминале где оно запущено).

### 3. Создайте миграцию

```bash
cd ArchiveWeb
dotnet ef migrations add InitialCreate
```

Или из корневой папки проекта:

```bash
dotnet ef migrations add InitialCreate --project ArchiveWeb
```

Это создаст папку `Migrations` в проекте `ArchiveWeb` с файлами миграции.

### 4. Примените миграции к базе данных

```bash
dotnet ef database update --project ArchiveWeb
```

Или из папки ArchiveWeb:

```bash
dotnet ef database update
```

### 5. Проверьте результат

После успешного применения миграций в базе данных `sachkov_tech` будут созданы следующие таблицы:

- `archive_configurations`
- `letters`
- `boxes`
- `file_archives`
- `archive_history`

### 6. Запустите приложение

```bash
dotnet run --project ArchiveWeb
```

Приложение должно запуститься без ошибок подключения к БД.

## Устранение проблем

### Ошибка "Build failed"

- Убедитесь, что приложение остановлено
- Выполните `dotnet clean` и затем `dotnet build`
- Проверьте, что все NuGet пакеты восстановлены: `dotnet restore`

### Ошибка подключения к БД

- Проверьте, что контейнер PostgreSQL запущен: `docker-compose ps`
- Проверьте логи: `docker-compose logs postgres`
- Убедитесь, что порт 5435 свободен и не занят другим процессом

### Ошибка "dotnet ef: command not found"

Установите EF Core Tools:

```bash
dotnet tool install --global dotnet-ef
```

