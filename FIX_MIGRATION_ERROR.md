# Решение проблемы с созданием миграции

## Проблема

При попытке создать миграцию возникает ошибка:
```
Build failed. Use dotnet build to see the errors.
```

Ошибка связана с тем, что **приложение запущено** и блокирует файл `ArchiveWeb.exe` (процесс 11044).

## Решение

### Шаг 1: Остановите запущенное приложение

**Вариант А: Через терминал**
- Найдите терминал, где запущено приложение (`dotnet run`)
- Нажмите `Ctrl+C` для остановки

**Вариант Б: Через диспетчер задач**
1. Откройте Диспетчер задач (Ctrl+Shift+Esc)
2. Найдите процесс `ArchiveWeb.exe` (PID 11044)
3. Завершите процесс

**Вариант В: Через PowerShell**
```powershell
Stop-Process -Id 11044 -Force
```

### Шаг 2: Обновите строку подключения (уже сделано)

Имя базы данных изменено с `sachkov_tech` на `archive` в:
- `appsettings.json` ✅
- `appsettings.Development.json` ✅

### Шаг 3: Создайте миграцию

После остановки приложения выполните:

```powershell
cd ArchiveWeb
dotnet ef migrations add InitialCreate
```

Или из корневой папки:

```powershell
dotnet ef migrations add InitialCreate --project ArchiveWeb
```

### Шаг 4: Примените миграции

```powershell
dotnet ef database update --project ArchiveWeb
```

## Проверка

После успешного выполнения команд:

1. Проверьте, что создана папка `ArchiveWeb/Migrations`
2. Убедитесь, что в ней есть файлы миграции
3. Проверьте подключение к БД, запустив приложение:

```powershell
dotnet run --project ArchiveWeb
```

## Альтернативный способ (если приложение все еще блокирует)

Если приложение не останавливается, можно использовать `dotnet clean`:

```powershell
cd ArchiveWeb
dotnet clean
dotnet build
dotnet ef migrations add InitialCreate
```

