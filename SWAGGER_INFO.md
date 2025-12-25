# Swagger UI - Документация API

## Доступ к Swagger UI

После запуска приложения Swagger UI будет доступен по адресу:

**Swagger UI:**
- Development: `https://localhost:5001/swagger` или `http://localhost:5000/swagger`

**Swagger JSON:**
- `https://localhost:5001/swagger/v1/swagger.json`

## Возможности Swagger UI

В Swagger UI доступны следующие функции:

1. **Просмотр всех эндпоинтов** - полный список доступных API методов
2. **Интерактивное тестирование** - возможность отправлять запросы прямо из браузера
3. **Документация моделей** - описание всех DTO и моделей данных
4. **Примеры запросов** - готовые примеры для каждого эндпоинта
5. **Валидация** - проверка корректности данных перед отправкой

## Текущие эндпоинты

### Абитуриенты (Applicants)

- `GET /api/applicants` - Получить список абитуриентов с пагинацией
- `GET /api/applicants/{id}` - Получить абитуриента по ID
- `POST /api/applicants` - Создать нового абитуриента
- `PUT /api/applicants/{id}` - Обновить данные абитуриента
- `DELETE /api/applicants/{id}` - Удалить абитуриента
- `GET /api/applicants/search` - Поиск абитуриентов

## Настройки Swagger

Swagger настроен в `Program.cs` со следующими параметрами:

- **RoutePrefix**: `swagger` - Swagger UI доступен по `/swagger`
- **DisplayRequestDuration**: Показывает время выполнения запроса
- **EnableTryItOutByDefault**: Кнопка "Try it out" включена по умолчанию
- **EnableDeepLinking**: Поддержка глубоких ссылок
- **EnableFilter**: Поиск и фильтрация эндпоинтов
- **ShowExtensions**: Показ расширенных возможностей
- **EnableValidator**: Валидация запросов

## XML комментарии

XML комментарии автоматически включаются в Swagger документацию благодаря настройке в `.csproj`:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

Все методы контроллеров документированы с помощью XML комментариев, которые отображаются в Swagger UI.

## Отключение Swagger в Production

Swagger UI включен только в режиме Development. В Production он автоматически отключен благодаря проверке:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(...);
}
```

## Расширенные возможности

Для расширенной настройки Swagger можно добавить:

- JWT аутентификацию в Swagger UI
- Кастомные схемы и примеры
- Настройку CORS для Swagger
- Группировку эндпоинтов по версиям API

