using System.Net;
using System.Text.Json;
using FluentValidation;
using NotificationHub.Shared.Exceptions;

namespace NotificationHub.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected — not an error
            return;
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors.Select(e => e.ErrorMessage).ToList();

            // Surface the first explicitly-set error code (e.g.
            // "invalid_reply_to") so clients can branch on it. FluentValidation's
            // built-in codes are PascalCase ("NotEmpty"), custom ones are
            // snake_case, which is what the underscore filter picks out.
            var code = ex.Errors
                .Select(e => e.ErrorCode)
                .FirstOrDefault(c => c.Contains('_', StringComparison.Ordinal));

            await WriteResponse(context, HttpStatusCode.BadRequest,
                new { error = errors.FirstOrDefault(), errors, code });
        }
        catch (NotFoundException ex)
        {
            await WriteResponse(context, HttpStatusCode.NotFound, new { error = ex.Message });
        }
        catch (DomainException ex)
        {
            await WriteResponse(context, HttpStatusCode.BadRequest, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteResponse(context, HttpStatusCode.InternalServerError,
                new { error = "An unexpected error occurred." });
        }
    }
    private static async Task WriteResponse(HttpContext context, HttpStatusCode statusCode, object body)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}