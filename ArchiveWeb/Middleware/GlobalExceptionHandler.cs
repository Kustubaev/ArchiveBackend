using ArchiveWeb.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace ArchiveWeb.Middleware;

public sealed class GlobalExceptionHandler : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанное исключение: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
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
                StatusCode = (int)HttpStatusCode.BadRequest,
                ErrorCode = "VALIDATION_ERROR",
                Message = "Ошибка валидации данных",
                Details = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray())
            },
            InvalidOperationException ex => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                ErrorCode = "INVALID_OPERATION",
                Message = ex.Message
            },
            _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                ErrorCode = "INTERNAL_ERROR",
                Message = "Внутренняя ошибка сервера"
            }
        };

        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = "application/json";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsJsonAsync(response, jsonOptions);
    }

    private sealed record ErrorResponse
    {
        public int StatusCode { get; init; }
        public string ErrorCode { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public Dictionary<string, string[]>? Details { get; init; }
    }
}

