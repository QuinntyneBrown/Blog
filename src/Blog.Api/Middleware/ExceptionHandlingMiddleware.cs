using Blog.Api.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Blog.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for request {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex, env);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex, IHostEnvironment env)
    {
        var (status, title, detail, errors) = ex switch
        {
            ValidationException ve => (400, "Validation Error", "One or more validation errors occurred.",
                ve.Errors.GroupBy(e => e.PropertyName.ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            BadRequestException bre => (400, "Bad Request", bre.Message, (Dictionary<string, string[]>?)null),
            UnauthorizedException ue => (401, "Unauthorized", ue.Message, (Dictionary<string, string[]>?)null),
            UnauthorizedAccessException => (401, "Unauthorized", "Authentication is required.", (Dictionary<string, string[]>?)null),
            NotFoundException nfe => (404, "Not Found", nfe.Message, (Dictionary<string, string[]>?)null),
            ConflictException ce => (409, "Conflict", ce.Message, (Dictionary<string, string[]>?)null),
            PreconditionFailedException pfe => (412, "Precondition Failed", pfe.Message, (Dictionary<string, string[]>?)null),
            FileTooLargeException fte => (413, "Payload Too Large", fte.Message, (Dictionary<string, string[]>?)null),
            RateLimitExceededException rle => (429, "Too Many Requests", rle.Message, (Dictionary<string, string[]>?)null),
            _ => (500, "Internal Server Error", env.IsDevelopment() ? ex.Message : "An unexpected error occurred.", (Dictionary<string, string[]>?)null)
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = GetTypeUri(status),
            title,
            status,
            detail,
            instance = context.Request.Path.Value,
            errors
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
    }

    private static string GetTypeUri(int status) => status switch
    {
        400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
        404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        412 => "https://tools.ietf.org/html/rfc7232#section-4.2",
        413 => "https://tools.ietf.org/html/rfc7231#section-6.5.11",
        429 => "https://tools.ietf.org/html/rfc6585#section-4",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
    };
}
