# Архивная система распределения дел - Backend Specification

## 1. Обзор системы

### 1.1. Назначение

Система для автоматического распределения дел абитуриентов по физическому архиву с учетом статистического распределения первых букв фамилий.

### 1.2. Ключевые возможности

- Автоматический расчет позиции дела в архиве на основе фамилии
- Адаптивное перераспределение дел с учетом фактической статистики
- Управление резервными местами в коробках
- Полное отслеживание истории перемещений дел
- Поддержка мягкого удаления и фактического изъятия дел

### 1.3. Технологический стек

- **Backend**: .NET 9, ASP.NET Core Web API
- **ORM**: Entity Framework Core 9
- **Database**: PostgreSQL 14+
- **Аутентификация**: JWT
- **Документация**: Swagger/OpenAPI 3.0
- **Контейнеризация**: Docker (опционально)

## 2. Архитектура

### 2.1. Слои приложения

```
Presentation Layer (API Controllers)
       ↓
Application Layer (Services, DTOs, Validators)
       ↓
Domain Layer (Entities, Business Rules, Interfaces)
       ↓
Infrastructure Layer (EF Core, Repositories, Migrations)
```

### 2.2. Сущности предметной области

#### 2.2.1. ArchiveConfiguration

Конфигурация архива

```csharp
public class ArchiveConfiguration : EntityBase
{
    public int BoxCapacity { get; set; } = 50;
    public int BoxReservePercent { get; set; } = 10;
    public int ArchiveReservePercent { get; set; } = 0;
    public int AdaptiveRedistributionThreshold { get; set; } = 90;
    public double AdaptiveWeightNew { get; set; } = 0.7;
    public double AdaptiveWeightOld { get; set; } = 0.3;
    public JsonDocument IdealDistribution { get; set; } // JSON с распределением букв
    public int TotalFilesForPlanning { get; set; } = 3000;
    public int EffectiveBoxCapacity { get; private set; } // Calculated: BoxCapacity * (100 - BoxReservePercent) / 100
}
```

#### 2.2.2. Letter

Буква с расчетными и фактическими данными

```csharp
public class Letter : EntityBase
{
    [Required, MaxLength(1)]
    public char Value { get; set; }
    
    public int? ExpectedCount { get; set; }      // Расчетное количество дел
    public int? StartBox { get; set; }          // Первая коробка для буквы
    public int? EndBox { get; set; }            // Последняя коробка
    public int? StartPosition { get; set; }     // Начальная позиция
    public int? EndPosition { get; set; }       // Конечная позиция
    public int ActualCount { get; set; } = 0;   // Фактическое количество дел
    
    // Навигационные свойства
    public ICollection<FileArchive> FileArchives { get; set; } = new List<FileArchive>();
    public ICollection<ArchiveHistory> HistoryEntries { get; set; } = new List<ArchiveHistory>();
    
    // Методы
    public bool IsOverflow() => ExpectedCount.HasValue && ActualCount >= ExpectedCount.Value;
}
```

#### 2.2.3. Box

Коробка в архиве

```csharp
public class Box : EntityBase
{
    [Required]
    public int Number { get; set; }              // Номер коробки (1-based)
    
    public int? ExpectedCount { get; set; }      // Эффективная вместимость
    public int CompletedCount { get; set; } = 0; // Заполнено дел
    
    // Навигационные свойства
    public ICollection<FileArchive> FileArchives { get; set; } = new List<FileArchive>();
    public ICollection<ArchiveHistory> HistoryEntries { get; set; } = new List<ArchiveHistory>();
    
    // Вычисляемые свойства
    public bool HasAvailableSpace => 
        ExpectedCount.HasValue && CompletedCount < ExpectedCount.Value;
    
    public int AvailableSpace => 
        ExpectedCount.HasValue ? ExpectedCount.Value - CompletedCount : 0;
}
```

#### 2.2.4. FileArchive

Дело в архиве

```csharp
public class FileArchive : EntityBase
{
    [Required, MaxLength(255)]
    public string Surname { get; set; }
    
    [Required]
    public int FileNumberForLetter { get; set; } // Порядковый номер для буквы
    
    [Required]
    public int PositionInBox { get; set; }       // Позиция в коробке (1-based)
    
    public bool IsDeleted { get; set; } = false; // Мягкое удаление
    
    // Внешние ключи
    [Required]
    public Guid ApplicantId { get; set; }        // ID из системы абитуриентов
    
    [Required]
    public Guid BoxId { get; set; }
    
    [Required]
    public Guid LetterId { get; set; }
    
    // Навигационные свойства
    public Box Box { get; set; }
    public Letter Letter { get; set; }
    public ICollection<ArchiveHistory> History { get; set; } = new List<ArchiveHistory>();
    
    // Вычисляемые свойства
    public string FileNumberForArchive
    {
        get
        {
            var orderNumbers = GetOrderNumbers();
            if (orderNumbers.TryGetValue(Letter.Value.ToString(), out string orderNumber))
            {
                return orderNumber + FileNumberForLetter.ToString("D3");
            }
            return "000" + FileNumberForLetter.ToString("D3");
        }
    }
}
```

#### 2.2.5. ArchiveHistory

История перемещений дел

```csharp
public class ArchiveHistory : EntityBase
{
    [Required]
    public HistoryAction Action { get; set; } // CREATE, MOVE, DELETE, REDISTRIBUTE
    
    public int? OldBoxNumber { get; set; }
    public int? OldPosition { get; set; }
    public int? NewBoxNumber { get; set; }
    public int? NewPosition { get; set; }
    
    [MaxLength(500)]
    public string Reason { get; set; } // Причина перемещения
    
    // Внешние ключи
    [Required]
    public Guid FileArchiveId { get; set; }
    
    public Guid? LetterId { get; set; }
    public Guid? BoxId { get; set; }
    
    // Навигационные свойства
    public FileArchive FileArchive { get; set; }
    public Letter Letter { get; set; }
    public Box Box { get; set; }
}

public enum HistoryAction
{
    Create = 1,     // Создание дела
    Move = 2,       // Перемещение
    Delete = 3,     // Мягкое удаление
    Remove = 4,     // Фактическое изъятие
    Redistribute = 5 // Автоматическое перераспределение
}
```

#### 2.2.6. EntityBase

Базовый класс для всех сущностей

```csharp
public abstract class EntityBase
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    [ConcurrencyCheck]
    public Guid Version { get; set; } = Guid.NewGuid(); // Для оптимистичной блокировки
}
```

## 3. Бизнес-логика

### 3.1. Инициализация архива

**Процесс:**

1. Создание конфигурации архива
2. Генерация букв на основе идеального распределения
3. Расчет ExpectedCount, StartBox, EndBox для каждой буквы
4. Создание коробок с EffectiveBoxCapacity
5. Создание специальной буквы "-" для переполнения

**Алгоритм расчета позиций букв:**

```csharp
public void CalculateLetterPositions(ArchiveConfiguration config, List<Letter> letters)
{
    int totalFiles = 0;
    int effectiveCapacity = config.EffectiveBoxCapacity;
    
    foreach (var letter in letters.OrderBy(l => l.Value))
    {
        // Расчет ожидаемого количества
        double percentage = GetPercentageFromIdealDistribution(letter.Value, config.IdealDistribution);
        letter.ExpectedCount = (int)Math.Round(config.TotalFilesForPlanning * percentage / 100.0);
        
        // Расчет коробок и позиций
        totalFiles += letter.ExpectedCount.Value;
        double boxCount = 1.0 * totalFiles / effectiveCapacity;
        
        letter.StartBox = (boxCount < Math.Ceiling(boxCount)) 
            ? (letters.LastOrDefault()?.EndBox ?? 0) 
            : (letters.LastOrDefault()?.EndBox ?? 0) + 1;
            
        letter.EndBox = (int)Math.Ceiling(boxCount);
        letter.StartPosition = ((letters.LastOrDefault()?.EndPosition ?? 0) + 1 <= effectiveCapacity) 
            ? (letters.LastOrDefault()?.EndPosition ?? 0) + 1 
            : 1;
            
        letter.EndPosition = (totalFiles % effectiveCapacity != 0) 
            ? totalFiles % effectiveCapacity 
            : effectiveCapacity;
    }
}
```

### 3.2. Добавление нового дела

**Поток выполнения:**

1. Получение данных абитуриента (ApplicantId, Surname)
2. Извлечение первой буквы фамилии (кириллица)
3. Поиск соответствующей буквы в системе
4. Проверка переполнения буквы → переход на букву "-"
5. Транзакционный расчет позиции
6. Создание FileArchive с рассчитанной позицией
7. Обновление счетчиков (Letter.ActualCount, Box.CompletedCount)
8. Запись в историю

**Алгоритм расчета позиции:**

```csharp
public async Task<FilePosition> CalculatePositionAsync(char letterValue, Guid letterId)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        // Оптимистичная блокировка через ConcurrencyCheck
        var letter = await _context.Letters
            .Where(l => l.Id == letterId)
            .FirstOrDefaultAsync();
            
        if (letter == null) throw new LetterNotFoundException(letterValue);
        
        // Проверка переполнения
        if (letter.IsOverflow())
        {
            letter = await GetOverflowLetterAsync();
        }
        
        // Расчет позиции
        var boxNumber = letter.StartBox.Value + 
            (int)Math.Floor((letter.StartPosition.Value - 1 + letter.ActualCount) / 
            (double)_config.EffectiveBoxCapacity);
            
        var position = ((letter.StartPosition.Value - 1 + letter.ActualCount) % 
            _config.EffectiveBoxCapacity) + 1;
            
        if (position == 0) position = _config.EffectiveBoxCapacity;
        
        // Получение коробки
        var box = await _context.Boxes
            .FirstOrDefaultAsync(b => b.Number == boxNumber);
            
        if (box == null || !box.HasAvailableSpace)
            throw new BoxFullException(boxNumber);
        
        // Обновление сущностей
        letter.ActualCount++;
        box.CompletedCount++;
        
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        
        return new FilePosition
        {
            BoxNumber = boxNumber,
            PositionInBox = position,
            FileNumberForLetter = letter.ActualCount
        };
    }
    catch (DbUpdateConcurrencyException)
    {
        await transaction.RollbackAsync();
        // Повторная попытка с экспоненциальной задержкой
        return await RetryCalculatePositionAsync(letterValue, letterId);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 3.3. Адаптивное перераспределение

**Триггеры:**

- Ручной запрос администратора через API
- Достижение порога заполнения (90%)

**Алгоритм перераспределения:**

```csharp
public async Task<RedistributionResult> RedistributeArchiveAsync(bool forceExpansion = false)
{
    // 1. Сбор статистики
    var actualDistribution = await CalculateActualDistributionAsync();
    var idealDistribution = LoadIdealDistribution(_config.IdealDistribution);
    
    // 2. Расчет нового распределения
    var newDistribution = CalculateAdaptiveDistribution(
        idealDistribution, 
        actualDistribution,
        _config.AdaptiveWeightOld,
        _config.AdaptiveWeightNew
    );
    
    // 3. Проверка необходимости расширения
    int totalFiles = await _context.FileArchives.CountAsync(f => !f.IsDeleted);
    int requiredCapacity = (int)(totalFiles * 1.1); // +10% резерв
    
    if (forceExpansion || requiredCapacity > _config.TotalFilesForPlanning)
    {
        await ExpandArchiveAsync(requiredCapacity);
    }
    
    // 4. Пересчет позиций букв
    var letters = await _context.Letters.ToListAsync();
    CalculateLetterPositions(_config, letters);
    
    // 5. Перераспределение существующих дел
    var redistributionMap = await RedistributeFilesAsync(letters);
    
    // 6. Запись в историю
    await LogRedistributionHistoryAsync(redistributionMap);
    
    return new RedistributionResult
    {
        TotalMovedFiles = redistributionMap.Count,
        NewDistribution = newDistribution,
        ArchiveExpanded = forceExpansion || requiredCapacity > _config.TotalFilesForPlanning
    };
}
```

**Формула адаптивного распределения:**

```
newPercentage[letter] = 
    (idealPercentage[letter] * adaptiveWeightOld) + 
    (actualPercentage[letter] * adaptiveWeightNew)
```

Где:
- `actualPercentage[letter] = (count[letter] / totalFiles) * 100`
- `adaptiveWeightOld + adaptiveWeightNew = 1.0`

### 3.4. Управление делами

**Мягкое удаление:**

- Установка `IsDeleted = true`
- Место в коробке остается занятым
- Дело исключается из поиска (фильтрация по `IsDeleted == false`)

**Фактическое изъятие:**

- Происходит только при перераспределении
- Дела с `IsDeleted == true` не переносятся
- Счетчики коробок и букв уменьшаются

## 4. API Спецификация

### 4.1. Базовая информация

- **Base URL**: `/api`
- **Аутентификация**: JWT Bearer Token
- **Формат данных**: JSON
- **Кодировка**: UTF-8

### 4.2. Эндпоинты

#### 4.2.1. Конфигурация архива

```http
GET /api/archive/configuration
Response: ArchiveConfigurationDto

PUT /api/archive/configuration
Body: UpdateArchiveConfigurationDto
Response: ArchiveConfigurationDto
```

#### 4.2.2. Управление делами

```http
POST /api/files
Body: CreateFileRequestDto
Response: FileArchiveDto (201 Created)

GET /api/files/{id}
Response: FileArchiveDto

GET /api/files
Query Parameters:
  - letter (optional): Фильтр по букве
  - boxNumber (optional): Фильтр по коробке
  - page (default: 1)
  - pageSize (default: 10)
Response: PagedResponse<FileArchiveDto>

DELETE /api/files/{id}
Response: 204 No Content (мягкое удаление)
```

#### 4.2.3. Перераспределение

```http
POST /api/archive/redistribute
Query Parameters:
  - forceExpansion (default: false)
Response: RedistributionResultDto

GET /api/archive/redistribute/status/{jobId}
Response: RedistributionStatusDto
```

#### 4.2.4. Поиск и статистика

```http
GET /api/archive/statistics
Response: ArchiveStatisticsDto

GET /api/search/by-surname
Query Parameters:
  - surname (required): Фамилия или часть
  - page (default: 1)
  - pageSize (default: 10)
Response: PagedResponse<FileArchiveDto>

GET /api/search/by-applicant/{applicantId}
Response: FileArchiveDto
```

#### 4.2.5. История

```http
GET /api/history/file/{fileId}
Query Parameters:
  - page (default: 1)
  - pageSize (default: 10)
Response: PagedResponse<ArchiveHistoryDto>

GET /api/history/box/{boxNumber}
Response: List<ArchiveHistoryDto>

GET /api/history/letter/{letter}
Response: List<ArchiveHistoryDto>
```

#### 4.2.6. Коробки

```http
GET /api/boxes
Query Parameters:
  - includeFiles (default: false)
Response: List<BoxDto>

GET /api/boxes/{number}
Response: BoxDto

GET /api/boxes/{number}/files
Response: List<FileArchiveDto>
```

#### 4.2.7. Буквы

```http
GET /api/letters
Response: List<LetterDto>

GET /api/letters/{letter}
Response: LetterDto

GET /api/letters/{letter}/files
Response: List<FileArchiveDto>

GET /api/letters/{letter}/statistics
Response: LetterStatisticsDto
```

### 4.3. DTO Модели

#### CreateFileRequestDto

```csharp
public class CreateFileRequestDto
{
    [Required]
    public Guid ApplicantId { get; set; }
    
    [Required]
    [RegularExpression(@"^[А-ЯЁ][а-яё]+$", ErrorMessage = "Фамилия должна начинаться с заглавной русской буквы")]
    [MaxLength(100)]
    public string Surname { get; set; }
}
```

#### FileArchiveDto

```csharp
public class FileArchiveDto
{
    public Guid Id { get; set; }
    public string Surname { get; set; }
    public char Letter { get; set; }
    public int FileNumberForLetter { get; set; }
    public string FileNumberForArchive { get; set; }
    public int BoxNumber { get; set; }
    public int PositionInBox { get; set; }
    public Guid ApplicantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

#### RedistributionResultDto

```csharp
public class RedistributionResultDto
{
    public Guid JobId { get; set; }
    public int TotalMovedFiles { get; set; }
    public bool ArchiveExpanded { get; set; }
    public Dictionary<char, double> NewDistribution { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public List<FileMoveDto> FileMoves { get; set; }
}

public class FileMoveDto
{
    public Guid FileId { get; set; }
    public string Surname { get; set; }
    public int OldBoxNumber { get; set; }
    public int OldPosition { get; set; }
    public int NewBoxNumber { get; set; }
    public int NewPosition { get; set; }
}
```

#### PagedResponse

```csharp
public class PagedResponse<T>
{
    public List<T> Items { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
```

## 5. База данных

### 5.1. Схема БД

```sql
-- Конфигурация архива
CREATE TABLE archive_configurations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    box_capacity INTEGER NOT NULL DEFAULT 50,
    box_reserve_percent INTEGER NOT NULL DEFAULT 10,
    archive_reserve_percent INTEGER NOT NULL DEFAULT 0,
    adaptive_redistribution_threshold INTEGER NOT NULL DEFAULT 90,
    adaptive_weight_new DECIMAL(3,2) NOT NULL DEFAULT 0.7,
    adaptive_weight_old DECIMAL(3,2) NOT NULL DEFAULT 0.3,
    ideal_distribution JSONB NOT NULL,
    total_files_for_planning INTEGER NOT NULL DEFAULT 3000,
    effective_box_capacity INTEGER NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    version UUID NOT NULL DEFAULT gen_random_uuid()
);

-- Буквы
CREATE TABLE letters (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    value CHAR(1) NOT NULL,
    expected_count INTEGER,
    start_box INTEGER,
    end_box INTEGER,
    start_position INTEGER,
    end_position INTEGER,
    actual_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    version UUID NOT NULL DEFAULT gen_random_uuid(),
    CONSTRAINT uk_letter_value UNIQUE (value)
);

-- Коробки
CREATE TABLE boxes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    number INTEGER NOT NULL,
    expected_count INTEGER,
    completed_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    version UUID NOT NULL DEFAULT gen_random_uuid(),
    CONSTRAINT uk_box_number UNIQUE (number)
);

-- Дела
CREATE TABLE file_archives (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    surname VARCHAR(255) NOT NULL,
    file_number_for_letter INTEGER NOT NULL,
    position_in_box INTEGER NOT NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    applicant_id UUID NOT NULL,
    box_id UUID NOT NULL REFERENCES boxes(id),
    letter_id UUID NOT NULL REFERENCES letters(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    version UUID NOT NULL DEFAULT gen_random_uuid(),
    CONSTRAINT uk_applicant UNIQUE (applicant_id) -- Одно дело на абитуриента
);

-- История
CREATE TABLE archive_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    action INTEGER NOT NULL,
    old_box_number INTEGER,
    old_position INTEGER,
    new_box_number INTEGER,
    new_position INTEGER,
    reason VARCHAR(500),
    file_archive_id UUID NOT NULL REFERENCES file_archives(id),
    letter_id UUID REFERENCES letters(id),
    box_id UUID REFERENCES boxes(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

-- Индексы для производительности
CREATE INDEX idx_file_archives_surname ON file_archives(surname);
CREATE INDEX idx_file_archives_box_id ON file_archives(box_id);
CREATE INDEX idx_file_archives_letter_id ON file_archives(letter_id);
CREATE INDEX idx_file_archives_is_deleted ON file_archives(is_deleted);
CREATE INDEX idx_archive_history_file_id ON archive_history(file_archive_id);
CREATE INDEX idx_archive_history_created_at ON archive_history(created_at DESC);
```

### 5.2. Миграции

Использование EF Core Migrations для управления схемой:

```bash
dotnet ef migrations add InitialCreate
dotnet ef migrations add AddArchiveHistory
dotnet ef database update
```

## 6. Аутентификация и безопасность

### 6.1. JWT Аутентификация

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Configuration["Jwt:Issuer"],
            ValidAudience = Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
        };
    });
```

### 6.2. Роли

Одна роль: **Administrator**

```csharp
[Authorize(Roles = "Administrator")]
public class FileArchiveController : ControllerBase
{
    // Все методы требуют роль администратора
}
```

### 6.3. Валидация

```csharp
public class CreateFileRequestValidator : AbstractValidator<CreateFileRequestDto>
{
    public CreateFileRequestValidator()
    {
        RuleFor(x => x.Surname)
            .NotEmpty()
            .Matches(@"^[А-ЯЁ][а-яё]+$")
            .WithMessage("Фамилия должна содержать только русские буквы и начинаться с заглавной");
            
        RuleFor(x => x.ApplicantId)
            .NotEmpty();
    }
}
```

## 7. Обработка ошибок

### 7.1. Кастомные исключения

```csharp
public class ArchiveException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }
    
    public ArchiveException(string message, string errorCode, int statusCode) 
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}

public class LetterNotFoundException : ArchiveException
{
    public LetterNotFoundException(char letter) 
        : base($"Буква '{letter}' не найдена", "LETTER_NOT_FOUND", 404) { }
}

public class BoxFullException : ArchiveException
{
    public BoxFullException(int boxNumber) 
        : base($"Коробка {boxNumber} заполнена", "BOX_FULL", 400) { }
}

public class InvalidSurnameException : ArchiveException
{
    public InvalidSurnameException(string surname) 
        : base($"Некорректная фамилия: {surname}", "INVALID_SURNAME", 400) { }
}
```

### 7.2. Global Exception Handler

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var response = exception switch
        {
            ArchiveException ex => new ErrorResponse
            {
                StatusCode = ex.StatusCode,
                ErrorCode = ex.ErrorCode,
                Message = ex.Message
            },
            ValidationException ex => new ErrorResponse
            {
                StatusCode = 400,
                ErrorCode = "VALIDATION_ERROR",
                Message = "Ошибка валидации",
                Details = ex.Errors
            },
            _ => new ErrorResponse
            {
                StatusCode = 500,
                ErrorCode = "INTERNAL_ERROR",
                Message = "Внутренняя ошибка сервера"
            }
        };
        
        httpContext.Response.StatusCode = response.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        
        return true;
    }
}
```

## 8. Развертывание

### 8.1. Требования к окружению

- **OS**: Windows/Linux/macOS
- **Runtime**: .NET 9.0
- **Database**: PostgreSQL 14+
- **Memory**: Минимум 512MB RAM
- **Storage**: Зависит от количества дел (примерно 1KB на дело)

### 8.2. Конфигурация

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ArchiveDB;Username=postgres;Password=password"
  },
  "Jwt": {
    "Key": "your-super-secret-key-min-32-chars",
    "Issuer": "ArchiveSystem",
    "Audience": "ArchiveClient",
    "ExpiryInMinutes": 60
  },
  "Archive": {
    "DefaultBoxCapacity": 50,
    "DefaultBoxReservePercent": 10,
    "DefaultAdaptiveThreshold": 90
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### 8.3. Docker Compose (опционально)

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:14-alpine
    environment:
      POSTGRES_DB: ArchiveDB
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      
  archive-api:
    build: .
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=ArchiveDB;Username=postgres;Password=password"
    ports:
      - "8080:8080"
    depends_on:
      - postgres

volumes:
  postgres_data:
```

## 9. Тестирование

### 9.1. Юнит-тесты

```csharp
public class PositionCalculatorTests
{
    [Theory]
    [InlineData('А', 1, 1, 45, 1, 1)] // Первое дело буквы А
    [InlineData('А', 45, 1, 45, 1, 45)] // Последнее дело в первой коробке
    [InlineData('А', 46, 1, 45, 2, 1)] // Первое дело во второй коробке
    public void CalculatePosition_ShouldReturnCorrectPosition(
        char letter, int actualCount, int expectedBox, int expectedPosition)
    {
        // Arrange
        var calculator = new PositionCalculator();
        var letterEntity = new Letter 
        { 
            Value = letter, 
            StartBox = 1, 
            StartPosition = 1,
            ActualCount = actualCount - 1
        };
        
        // Act
        var position = calculator.CalculatePosition(letterEntity, 45);
        
        // Assert
        Assert.Equal(expectedBox, position.BoxNumber);
        Assert.Equal(expectedPosition, position.PositionInBox);
    }
}
```

### 9.2. Интеграционные тесты

```csharp
public class FileArchiveIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task CreateFile_ShouldCalculateCorrectPosition()
    {
        // Arrange
        var request = new CreateFileRequestDto
        {
            ApplicantId = Guid.NewGuid(),
            Surname = "Иванов"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/files", request);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var file = await response.Content.ReadFromJsonAsync<FileArchiveDto>();
        
        Assert.Equal('И', file.Letter);
        Assert.InRange(file.BoxNumber, 1, 100);
        Assert.InRange(file.PositionInBox, 1, 45);
        Assert.False(file.IsDeleted);
    }
}
```

## 10. Мониторинг и логирование

### 10.1. Health Checks

```csharp
services.AddHealthChecks()
    .AddDbContextCheck<ArchiveDbContext>()
    .AddNpgSql(Configuration.GetConnectionString("DefaultConnection"));
```

### 10.2. Structured Logging

```csharp
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
    options.JsonWriterOptions = new JsonWriterOptions { Indented = true };
});
```

## 11. Дальнейшее развитие

### 11.1. Планируемые улучшения

- **Батчинг операций**: Массовое добавление/удаление дел
- **Расширенная аналитика**: Прогнозы заполнения, рекомендации
- **Интеграция с печатью**: Генерация этикеток для коробок
- **Мобильное приложение**: Сканирование QR-кодов коробок
- **Репликация БД**: Для отказоустойчивости

### 11.2. Резервное копирование

- Ежедневный экспорт структуры архива
- Миграционные скрипты между версиями
- Валидация целостности данных

## 📋 Чек-лист запуска проекта

1. Создать проект ASP.NET Core Web API (.NET 9)
2. Установить NuGet пакеты:
   - Microsoft.EntityFrameworkCore
   - Npgsql.EntityFrameworkCore.PostgreSQL
   - Swashbuckle.AspNetCore
   - Microsoft.AspNetCore.Authentication.JwtBearer
   - FluentValidation.AspNetCore
3. Настроить конфигурацию в `appsettings.json`
4. Реализовать сущности и DbContext
5. Создать миграцию и обновить БД
6. Реализовать сервисы бизнес-логики
7. Создать контроллеры с валидацией
8. Настроить JWT аутентификацию
9. Реализовать обработку ошибок
10. Написать базовые тесты
11. Настроить Swagger документацию
12. Протестировать основные сценарии
