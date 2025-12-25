# Архивная система распределения дел

Backend система для автоматического распределения дел абитуриентов по физическому архиву.

## Требования

- .NET 9.0 SDK
- Docker и Docker Compose
- PostgreSQL 14+ (разворачивается через Docker)

## Быстрый старт

### 1. Развертывание базы данных PostgreSQL

Запустите PostgreSQL контейнер:

```bash
docker-compose up -d
```

Проверьте статус контейнера:

```bash
docker-compose ps
```

Проверьте логи (если нужно):

```bash
docker-compose logs postgres
```

Остановка контейнера:

```bash
docker-compose down
```

Остановка с удалением данных:

```bash
docker-compose down -v
```

### 2. Настройка проекта

Восстановите зависимости:

```bash
dotnet restore
```

### 3. Применение миграций

Создайте первую миграцию:

```bash
dotnet ef migrations add InitialCreate --project ArchiveWeb
```

Примените миграции к базе данных:

```bash
dotnet ef database update --project ArchiveWeb
```

### 4. Запуск приложения

```bash
dotnet run --project ArchiveWeb
```

Приложение будет доступно по адресу: `https://localhost:5001` или `http://localhost:5000`

Swagger UI доступен по адресу: `https://localhost:5001/swagger` (в режиме Development)

## Структура проекта

```
ArchiveWeb/
├── Domain/              # Доменный слой
│   └── Entities/       # Сущности предметной области
├── Infrastructure/     # Слой инфраструктуры
│   └── Data/           # DbContext и конфигурация БД
├── Application/        # Слой приложения (будущий)
│   ├── Services/       # Сервисы бизнес-логики
│   ├── DTOs/           # Data Transfer Objects
│   └── Validators/     # Валидаторы
└── Controllers/        # API контроллеры (будущий)
```

## Параметры подключения к БД

- **Host**: localhost
- **Port**: 5435
- **Database**: sachkov_tech
- **Username**: postgres
- **Password**: postgres

## Полезные команды

### Работа с миграциями

```bash
# Создать новую миграцию
dotnet ef migrations add MigrationName --project ArchiveWeb

# Применить все миграции
dotnet ef database update --project ArchiveWeb

# Откатить последнюю миграцию
dotnet ef database update PreviousMigrationName --project ArchiveWeb

# Удалить последнюю миграцию (если еще не применена)
dotnet ef migrations remove --project ArchiveWeb

# Список всех миграций
dotnet ef migrations list --project ArchiveWeb
```

### Работа с Docker

```bash
# Запуск в фоновом режиме
docker-compose up -d

# Просмотр логов
docker-compose logs -f postgres

# Остановка
docker-compose stop

# Перезапуск
docker-compose restart

# Полная остановка с удалением контейнеров
docker-compose down

# Остановка с удалением данных
docker-compose down -v
```

## Дальнейшие шаги

1. ✅ Создана структура проекта
2. ✅ Реализованы доменные сущности
3. ✅ Настроен DbContext
4. ⏳ Создание миграций
5. ⏳ Реализация сервисов бизнес-логики
6. ⏳ Создание API контроллеров
7. ⏳ Настройка аутентификации JWT
8. ⏳ Обработка ошибок

