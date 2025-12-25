# Настройка и использование Swagger

## Что было сделано

1. ✅ Добавлен пакет `Swashbuckle.AspNetCore` версии 6.9.0
2. ✅ Настроена генерация XML комментариев для документации API
3. ✅ Настроен Swagger UI с подробной документацией
4. ✅ Добавлены атрибуты `ProducesResponseType` для всех эндпоинтов
5. ✅ Улучшена XML документация методов контроллера

## Доступ к Swagger UI

После запуска приложения Swagger UI будет доступен по адресу:

**В режиме Development:**
- `http://localhost:5000` или `https://localhost:5001` (корневой путь)
- `http://localhost:5000/swagger` или `https://localhost:5001/swagger` (альтернативный путь)

**Swagger JSON:**
- `http://localhost:5000/swagger/v1/swagger.json`

## Возможности Swagger UI

- ✅ Интерактивное тестирование API
- ✅ Автоматическая генерация документации из XML комментариев
- ✅ Примеры запросов и ответов
- ✅ Описание всех параметров и моделей данных
- ✅ Коды ответов с описаниями
- ✅ Валидация запросов перед отправкой

## Примеры использования

### 1. Создание абитуриента

В Swagger UI:
1. Найдите эндпоинт `POST /api/applicants`
2. Нажмите "Try it out"
3. Заполните JSON тело запроса:

```json
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

4. Нажмите "Execute"
5. Проверьте ответ (должен быть 201 Created)

### 2. Получение списка абитуриентов

1. Найдите эндпоинт `GET /api/applicants`
2. Нажмите "Try it out"
3. Установите параметры:
   - `page`: 1
   - `pageSize`: 10
4. Нажмите "Execute"

### 3. Поиск абитуриентов

1. Найдите эндпоинт `GET /api/applicants/search`
2. Нажмите "Try it out"
3. Установите параметры:
   - `query`: "Иванов"
   - `page`: 1
   - `pageSize`: 10
4. Нажмите "Execute"

## Значения Enums

### EducationLevel (Уровень образования)
- `1` - SecondaryProfessional (Среднее профессиональное образование)
- `2` - BachelorOrSpecialist (Бакалавриат / Специалитет)
- `3` - Master (Магистратура)
- `4` - Postgraduate (Аспирантура)

### StudyForm (Форма обучения)
- `1` - FullTime (Очная форма обучения)
- `2` - MixedFullPartTime (Очно-заочная форма обучения)
- `3` - PartTime (Заочная форма обучения)

## Настройки Swagger

Swagger настроен в `Program.cs` со следующими параметрами:

- **RoutePrefix**: `string.Empty` - Swagger UI доступен по корневому пути
- **DisplayRequestDuration**: включено - показывает время выполнения запроса
- **EnableTryItOutByDefault**: включено - кнопка "Try it out" активна по умолчанию

## Генерация документации

XML комментарии автоматически включаются в Swagger документацию благодаря настройке в `.csproj`:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

Все методы контроллера документированы с помощью XML комментариев (`/// <summary>`) и атрибутов `ProducesResponseType`.

## Отключение Swagger в Production

Swagger UI включен только в режиме Development. В Production он автоматически отключен благодаря проверке:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(...);
}
```

## Дополнительные возможности

Для расширенной настройки Swagger можно добавить:

- JWT аутентификацию в Swagger UI
- Кастомные схемы валидации
- Примеры ответов для разных сценариев
- Группировку эндпоинтов по тегам
- Настройку CORS для Swagger

