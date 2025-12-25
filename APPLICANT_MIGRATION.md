# Миграция для добавления таблицы Applicants

После добавления сущности `Applicant` необходимо создать и применить миграцию.

## Шаги

### 1. Убедитесь, что приложение остановлено

Если приложение запущено, остановите его (Ctrl+C).

### 2. Создайте миграцию

```powershell
cd ArchiveWeb
dotnet ef migrations add AddApplicantsTable
```

Или из корневой папки:

```powershell
dotnet ef migrations add AddApplicantsTable --project ArchiveWeb
```

### 3. Примените миграцию

```powershell
dotnet ef database update --project ArchiveWeb
```

## Что будет создано

Миграция создаст таблицу `applicants` со следующими полями:

- `id` (UUID, PRIMARY KEY)
- `full_name` (VARCHAR(255), NOT NULL)
- `education_level` (INTEGER, NOT NULL)
- `study_form` (INTEGER, NOT NULL)
- `is_original_submitted` (BOOLEAN, NOT NULL, DEFAULT FALSE)
- `is_budget_financing` (BOOLEAN, NOT NULL)
- `phone_number` (VARCHAR(50), NOT NULL)
- `email` (VARCHAR(255), NOT NULL, UNIQUE)
- `created_at` (TIMESTAMPTZ, NOT NULL)
- `updated_at` (TIMESTAMPTZ, NULLABLE)
- `version` (UUID, NOT NULL, для оптимистичной блокировки)

**Индексы:**
- Уникальный индекс на `email` (uk_applicant_email)
- Индекс на `phone_number` (idx_applicant_phone)

## API Эндпоинты

После применения миграции будут доступны следующие эндпоинты:

### GET /api/applicants
Получить список абитуриентов с пагинацией
- Query параметры: `page` (default: 1), `pageSize` (default: 10)

### GET /api/applicants/{id}
Получить абитуриента по ID

### POST /api/applicants
Создать нового абитуриента
- Body: `CreateApplicantDto`

### PUT /api/applicants/{id}
Обновить данные абитуриента
- Body: `UpdateApplicantDto`

### DELETE /api/applicants/{id}
Удалить абитуриента (только если нет связанного дела)

### GET /api/applicants/search
Поиск абитуриентов по имени, email или телефону
- Query параметры: `query`, `page`, `pageSize`

## Примеры запросов

### Создание абитуриента

```http
POST /api/applicants
Content-Type: application/json

{
  "fullName": "Иванов Иван Иванович",
  "educationLevel": 3,
  "studyForm": 1,
  "isOriginalSubmitted": true,
  "isBudgetFinancing": true,
  "phoneNumber": "+7 (999) 123-45-67",
  "email": "ivanov@example.com"
}
```

### Поиск абитуриентов

```http
GET /api/applicants/search?query=Иванов&page=1&pageSize=10
```

### Получение списка с пагинацией

```http
GET /api/applicants?page=1&pageSize=20
```

