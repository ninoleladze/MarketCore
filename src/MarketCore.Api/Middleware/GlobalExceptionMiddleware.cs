using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace MarketCore.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation exception caught by global handler");
            await WriteProblemAsync(context, ex,
                statusCode: (int)HttpStatusCode.UnprocessableEntity,
                title: "Validation Failed",
                detail: string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access exception caught by global handler");
            await WriteProblemAsync(context, ex,
                statusCode: (int)HttpStatusCode.Unauthorized,
                title: "Unauthorized",
                detail: ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found exception caught by global handler");
            await WriteProblemAsync(context, ex,
                statusCode: (int)HttpStatusCode.NotFound,
                title: "Resource Not Found",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception caught by global handler for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteProblemAsync(context, ex,
                statusCode: (int)HttpStatusCode.InternalServerError,
                title: "An unexpected error occurred",
                detail: ex.Message + (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : ""));
        }
    }

    private async Task WriteProblemAsync(
        HttpContext context,
        Exception ex,
        int statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var correlationId = context.Items["CorrelationId"]?.ToString()
            ?? context.TraceIdentifier;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions =
            {
                ["correlationId"] = correlationId,
                ["traceId"] = context.TraceIdentifier
            }
        };

        if (_environment.IsDevelopment())
        {
            problem.Extensions["exceptionType"] = ex.GetType().FullName;
            problem.Extensions["exceptionMessage"] = ex.Message;

            if (ex.InnerException is not null)
            {
                problem.Extensions["innerException"] =
                    $"{ex.InnerException.GetType().FullName}: {ex.InnerException.Message}";
            }

            problem.Extensions["stackTrace"] = ex.StackTrace;
        }

        var json = JsonSerializer.Serialize(problem, JsonOptions);
        await context.Response.WriteAsync(json);
    }
}
