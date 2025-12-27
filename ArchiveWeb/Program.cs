using ArchiveWeb.Application.Services;
using ArchiveWeb.Application.Validators;
using ArchiveWeb.Domain.Interfaces;
using ArchiveWeb.Domain.Interfaces.Services;
using ArchiveWeb.Infrastructure;
using ArchiveWeb.Infrastructure.Data;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Подключение FluentValidation
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters();

// Регистрация валидаторов
builder.Services.AddValidatorsFromAssemblyContaining<CreateApplicantDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<InitializeArchiveDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateApplicantDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateArchiveConfigurationDtoValidator>();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Архивная система распределения дел",
        Description = "API для автоматического распределения дел абитуриентов по физическому архиву с учетом статистического распределения первых букв фамилий."
    });

    // Включаем XML комментарии для документации
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Настройка для работы с enums
    options.UseInlineDefinitionsForEnums();
    
    // Группировка по тегам
    options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "Default" });
    options.DocInclusionPredicate((name, api) => true);
    
    // Использование полных имен для избежания конфликтов
    options.CustomSchemaIds(type => type.FullName);
});

// Database
builder.Services.AddDbContext<ArchiveDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ArchiveDbContext>()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
        name: "postgresql");

// Application Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IApplicantService, ApplicantService>();
builder.Services.AddScoped<IArchiveService, ArchiveService>();
builder.Services.AddScoped<IFileArchiveService, FileArchiveService>();
builder.Services.AddScoped<ILetterService, LetterService>();
builder.Services.AddScoped<IBoxService, BoxService>();
builder.Services.AddScoped<IHistoryService, HistoryService>();

// Exception Handling
builder.Services.AddScoped<ArchiveWeb.Middleware.GlobalExceptionHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Archive System API v1");
        options.RoutePrefix = "swagger"; // Swagger UI доступен по /swagger
        options.DisplayRequestDuration();
        options.EnableTryItOutByDefault();
        options.EnableDeepLinking();
        options.EnableFilter();
        options.ShowExtensions();
        options.EnableValidator();
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    });
}

app.UseHttpsRedirection();

// Exception Handling Middleware
app.UseMiddleware<ArchiveWeb.Middleware.GlobalExceptionHandler>();

app.UseAuthorization();

// Health Checks endpoint
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
